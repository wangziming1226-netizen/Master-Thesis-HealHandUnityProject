using System;
using System.Collections.Generic;
using UnityEngine;

namespace Thesis.Modules.Practice
{
    [Serializable]
    public class AdaptiveTaskResult
    {
        public string gestureType;
        public bool success;
        public float score;
        public TrainingErrorType errorType;
        public float trackingRatio;
        public float strongMatchRatio;
        public float completionTimeSec;
        public string timestampUtc;
    }

    /// Lightweight, rule-based adaptive difficulty controller.
    /// The controller uses recent valid task results to decide whether task
    /// difficulty should increase. It never lowers difficulty automatically:
    /// when performance does not meet the level-up rules, the current settings
    /// are kept unchanged.
    public class AdaptiveDifficultyController : MonoBehaviour
    {
        [Header("Mode")]
        public bool adaptiveEnabled = true;
        public bool debugLogs = true;

        [Header("Target")]
        public PracticeModeController targetPracticeController;

        [Header("Window / Guardrails")]
        [Min(2)] public int recentWindowSize = 5;
        [Min(1)] public int minValidTasksBeforeAdjust = 2;
        [Range(0f, 1f)] public float validTrackingRatioThreshold = 0.45f;
        [Min(0)] public int cooldownTasksAfterAdjustment = 0;

        [Header("Level-Up Rules")]
        public bool requireStrongMatchRatioForLevelUp = false;
        public float levelUpAvgScore3 = 80f;
        public float levelUpAvgStrongRatio3 = 0.55f;
        public float levelUpAvgScore2 = 88f;
        public float levelUpAvgStrongRatio2 = 0.70f;

        [Header("Adaptive Parameters - Hold Time")]
        public float minHoldSeconds = 2.0f;
        public float maxHoldSeconds = 4.0f;
        public float holdSecondsStep = 0.25f;

        [Header("Adaptive Parameters - Strong Threshold")]
        public float minStrongThreshold = 0.55f;
        public float maxStrongThreshold = 0.72f;
        public float strongThresholdStep = 0.02f;

        [Header("Adaptive Parameters - Weak Threshold")]
        public float minWeakThreshold = 0.30f;
        public float maxWeakThreshold = 0.55f;
        public float weakThresholdStep = 0.02f;

        [Header("Threshold Safety")]
        public float minGapBetweenStrongAndWeak = 0.10f;

        [Header("Runtime State")]
        public float currentHoldSeconds;
        public float currentStrongThreshold;
        public float currentWeakThreshold;

        [TextArea] public string lastDecision = "None";
        public int cooldownRemaining = 0;
        public int totalAdjustments = 0;
        public int harderCount = 0;

        [Header("Runtime Debug")]
        public int totalResultsStored = 0;
        public int validResultsInWindow = 0;
        public float lastWindowAvgScore = 0f;
        public float lastWindowAvgStrongRatio = 0f;

        private readonly List<AdaptiveTaskResult> recentResults = new List<AdaptiveTaskResult>();

        private void Awake()
        {
#if UNITY_2023_1_OR_NEWER
            if (targetPracticeController == null)
            {
                var allControllers = FindObjectsByType<PracticeModeController>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

                if (allControllers != null && allControllers.Length > 0)
                    targetPracticeController = allControllers[0];
            }
#else
            if (targetPracticeController == null)
                targetPracticeController = FindObjectOfType<PracticeModeController>();
#endif
            SyncFromPractice();
        }

        /// Reads the current task settings from the linked Practice controller.
        public void SyncFromPractice()
        {
            if (targetPracticeController == null)
                return;

            currentHoldSeconds = targetPracticeController.targetHoldSeconds;
            currentStrongThreshold = targetPracticeController.strongMatchThreshold;
            currentWeakThreshold = targetPracticeController.weakMatchThreshold;

            ClampCurrentParams();
        }

        /// Starts a new adaptive session from the QR-card or mode-provided base settings.
        public void ResetAdaptiveSession(float baseHold, float baseStrong, float baseWeak)
        {
            recentResults.Clear();

            totalResultsStored = 0;
            validResultsInWindow = 0;
            lastWindowAvgScore = 0f;
            lastWindowAvgStrongRatio = 0f;

            cooldownRemaining = 0;
            totalAdjustments = 0;
            harderCount = 0;
            lastDecision = "None";

            currentHoldSeconds = baseHold;
            currentStrongThreshold = baseStrong;
            currentWeakThreshold = baseWeak;

            ClampCurrentParams();
            ApplyToPractice();
        }

        /// Pushes the current adaptive parameters to the linked Practice controller.
        public void ApplyToPractice()
        {
            if (targetPracticeController == null)
                return;

            targetPracticeController.targetHoldSeconds = currentHoldSeconds;
            targetPracticeController.strongMatchThreshold = currentStrongThreshold;
            targetPracticeController.weakMatchThreshold = currentWeakThreshold;

            if (debugLogs)
            {
                Debug.Log(
                    $"[Adaptive] ApplyToPractice -> hold={currentHoldSeconds:0.00}, " +
                    $"strong={currentStrongThreshold:0.00}, weak={currentWeakThreshold:0.00}",
                    this
                );
            }
        }

        /// Stores a task result, checks recent valid performance, and either
        /// raises the difficulty or keeps the current parameters unchanged.
        public void RegisterTaskResult(AdaptiveTaskResult result)
        {
            if (result == null)
                return;

            recentResults.Add(result);

            if (recentResults.Count > recentWindowSize)
                recentResults.RemoveAt(0);

            totalResultsStored = recentResults.Count;

            if (debugLogs)
            {
                Debug.Log(
                    $"[Adaptive] Add result -> success={result.success}, score={result.score:0.0}, " +
                    $"tracking={result.trackingRatio:0.00}, strong={result.strongMatchRatio:0.00}, " +
                    $"error={result.errorType}, stored={totalResultsStored}",
                    this
                );
            }

            if (!adaptiveEnabled)
            {
                lastDecision = "Adaptive disabled";
                return;
            }

            List<AdaptiveTaskResult> validResults = GetValidRecentResults();
            validResultsInWindow = validResults.Count;

            if (validResults.Count > 0)
            {
                lastWindowAvgScore = AverageScore(validResults);
                lastWindowAvgStrongRatio = AverageStrongRatio(validResults);
            }
            else
            {
                lastWindowAvgScore = 0f;
                lastWindowAvgStrongRatio = 0f;
            }

            if (validResults.Count < minValidTasksBeforeAdjust)
            {
                lastDecision = $"Not enough valid tasks ({validResults.Count}/{minValidTasksBeforeAdjust})";
                return;
            }

            if (cooldownRemaining > 0)
            {
                cooldownRemaining--;
                lastDecision = $"Cooldown ({cooldownRemaining} left)";
                return;
            }

            bool shouldMakeHarder = ShouldMakeHarder(validResults);

            if (shouldMakeHarder)
            {
                MakeHarder();
                ApplyToPractice();

                cooldownRemaining = cooldownTasksAfterAdjustment;
                totalAdjustments++;
                harderCount++;
                lastDecision = "Harder";
            }
            else
            {
                lastDecision = "Keep";
            }

            if (debugLogs)
            {
                Debug.Log(
                    $"[Adaptive] decision={lastDecision}, valid={validResults.Count}, " +
                    $"avgScore={lastWindowAvgScore:0.0}, avgStrong={lastWindowAvgStrongRatio:0.00}, " +
                    $"hold={currentHoldSeconds:0.00}, strong={currentStrongThreshold:0.00}, " +
                    $"weak={currentWeakThreshold:0.00}",
                    this
                );
            }
        }

        /// Keeps only results with sufficient tracking quality for adaptation.
        private List<AdaptiveTaskResult> GetValidRecentResults()
        {
            List<AdaptiveTaskResult> validResults = new List<AdaptiveTaskResult>();

            for (int i = 0; i < recentResults.Count; i++)
            {
                if (recentResults[i].trackingRatio >= validTrackingRatioThreshold)
                    validResults.Add(recentResults[i]);
            }

            return validResults;
        }

        /// Returns true when recent valid performance satisfies either a
        /// three-task or a two-task level-up rule.
        private bool ShouldMakeHarder(List<AdaptiveTaskResult> validResults)
        {
            List<AdaptiveTaskResult> last3 = GetLast(validResults, 3);
            List<AdaptiveTaskResult> last2 = GetLast(validResults, 2);

            bool threeTaskRule =
                last3.Count == 3 &&
                AllSuccess(last3) &&
                AverageScore(last3) >= levelUpAvgScore3 &&
                (!requireStrongMatchRatioForLevelUp ||
                 AverageStrongRatio(last3) >= levelUpAvgStrongRatio3);

            bool twoTaskRule =
                last2.Count == 2 &&
                AllSuccess(last2) &&
                AverageScore(last2) >= levelUpAvgScore2 &&
                (!requireStrongMatchRatioForLevelUp ||
                 AverageStrongRatio(last2) >= levelUpAvgStrongRatio2);

            return threeTaskRule || twoTaskRule;
        }

        /// Raises the active task parameters within their configured bounds.
        private void MakeHarder()
        {
            currentHoldSeconds += holdSecondsStep;
            currentStrongThreshold += strongThresholdStep;
            currentWeakThreshold += weakThresholdStep;

            ClampCurrentParams();

            if (targetPracticeController != null)
            {
                targetPracticeController.currentDifficultyLevel = Mathf.Clamp(
                    targetPracticeController.currentDifficultyLevel + 1,
                    1,
                    4
                );
            }
        }

        /// Keeps every parameter within its configured range and preserves the
        /// minimum gap between strong and weak thresholds.
        private void ClampCurrentParams()
        {
            currentHoldSeconds = Mathf.Clamp(currentHoldSeconds, minHoldSeconds, maxHoldSeconds);
            currentStrongThreshold = Mathf.Clamp(
                currentStrongThreshold,
                minStrongThreshold,
                maxStrongThreshold
            );
            currentWeakThreshold = Mathf.Clamp(
                currentWeakThreshold,
                minWeakThreshold,
                maxWeakThreshold
            );

            if (currentWeakThreshold > currentStrongThreshold - minGapBetweenStrongAndWeak)
                currentWeakThreshold = currentStrongThreshold - minGapBetweenStrongAndWeak;

            currentWeakThreshold = Mathf.Clamp(
                currentWeakThreshold,
                minWeakThreshold,
                maxWeakThreshold
            );
        }

        /// Returns the latest n results from a source list.
        private List<AdaptiveTaskResult> GetLast(List<AdaptiveTaskResult> source, int n)
        {
            List<AdaptiveTaskResult> results = new List<AdaptiveTaskResult>();

            if (source == null || source.Count == 0 || n <= 0)
                return results;

            int startIndex = Mathf.Max(0, source.Count - n);

            for (int i = startIndex; i < source.Count; i++)
                results.Add(source[i]);

            return results;
        }

        /// Returns true only if every result in the list is successful.
        private bool AllSuccess(List<AdaptiveTaskResult> results)
        {
            if (results == null || results.Count == 0)
                return false;

            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i].success)
                    return false;
            }

            return true;
        }

        /// Calculates the mean quality score for a list of task results.
        private float AverageScore(List<AdaptiveTaskResult> results)
        {
            if (results == null || results.Count == 0)
                return 0f;

            float sum = 0f;

            for (int i = 0; i < results.Count; i++)
                sum += results[i].score;

            return sum / results.Count;
        }


        /// Calculates the mean strong-match ratio for a list of task results.
        private float AverageStrongRatio(List<AdaptiveTaskResult> results)
        {
            if (results == null || results.Count == 0)
                return 0f;

            float sum = 0f;

            for (int i = 0; i < results.Count; i++)
                sum += results[i].strongMatchRatio;

            return sum / results.Count;
        }
    }
}
