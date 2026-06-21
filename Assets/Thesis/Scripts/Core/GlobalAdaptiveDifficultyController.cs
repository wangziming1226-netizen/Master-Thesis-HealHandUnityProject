using System.Collections.Generic;
using UnityEngine;

namespace Thesis.Core.Adaptive
{
    public class GlobalAdaptiveDifficultyController : MonoBehaviour
    {
        [Header("Mode")]
        public bool adaptiveEnabled = true;
        public bool debugLogs = true;

        [Header("Targets")]
        public AdaptiveModeAdapterBase[] adapters;
        public AdaptiveModeAdapterBase currentTarget;

        [Header("Window / Guardrails")]
        [Min(2)] public int recentWindowSize = 5;
        [Min(1)] public int minValidTasksBeforeAdjust = 2;
        [Range(0f, 1f)] public float validTrackingRatioThreshold = 0.60f;
        [Min(0)] public int cooldownTasksAfterAdjustment = 2;

        [Header("Upgrade Rules")]
        public float levelUpAvgScore3 = 88f;
        public float levelUpAvgStrongRatio3 = 0.75f;
        public float levelUpAvgScore2 = 93f;
        public float levelUpAvgStrongRatio2 = 0.85f;

        [Header("Downgrade Rules")]
        public float levelDownAvgScore3 = 45f;

        [Header("Adaptive Params - Hold Time")]
        public float minHoldSeconds = 2.0f;
        public float maxHoldSeconds = 4.0f;
        public float holdSecondsStep = 0.25f;

        [Header("Adaptive Params - Strong Threshold")]
        public float minStrongThreshold = 0.55f;
        public float maxStrongThreshold = 0.72f;
        public float strongThresholdStep = 0.02f;

        [Header("Adaptive Params - Weak Threshold")]
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
        public int easierCount = 0;

        [Header("Runtime Debug")]
        public int totalResultsStored = 0;
        public int validResultsInWindow = 0;
        public float lastWindowAvgScore = 0f;
        public float lastWindowAvgStrongRatio = 0f;

        private readonly List<AdaptiveTaskResult> recentResults = new List<AdaptiveTaskResult>();

        void Awake()
        {
            RefreshCurrentTarget();
            SyncFromTarget();
        }

        public void RefreshCurrentTarget()
        {
            if (adapters == null || adapters.Length == 0) return;

            for (int i = 0; i < adapters.Length; i++)
            {
                if (adapters[i] != null && adapters[i].IsModeActive)
                {
                    currentTarget = adapters[i];
                    return;
                }
            }

            if (currentTarget == null)
            {
                for (int i = 0; i < adapters.Length; i++)
                {
                    if (adapters[i] != null)
                    {
                        currentTarget = adapters[i];
                        return;
                    }
                }
            }
        }

        public void SyncFromTarget()
        {
            if (currentTarget == null) return;

            AdaptiveParams p = currentTarget.GetCurrentParams();
            currentHoldSeconds = p.holdSeconds;
            currentStrongThreshold = p.strongThreshold;
            currentWeakThreshold = p.weakThreshold;

            ClampCurrentParams();
        }

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
            easierCount = 0;

            lastDecision = "None";

            currentHoldSeconds = baseHold;
            currentStrongThreshold = baseStrong;
            currentWeakThreshold = baseWeak;

            ClampCurrentParams();
            ApplyToCurrentTarget();
        }

        public void SetAdaptiveModeForCurrent(bool enabled)
        {
            adaptiveEnabled = enabled;

            RefreshCurrentTarget();
            if (currentTarget != null)
                currentTarget.SetAdaptiveMode(enabled);

            lastDecision = enabled ? "Adaptive enabled" : "Adaptive disabled";
        }

        public void ApplyToCurrentTarget()
        {
            RefreshCurrentTarget();
            if (currentTarget == null) return;

            AdaptiveParams p = new AdaptiveParams(
                currentHoldSeconds,
                currentStrongThreshold,
                currentWeakThreshold
            );

            currentTarget.ApplyAdaptiveParams(p);

            if (debugLogs)
            {
                Debug.Log(
                    $"[GlobalAdaptive] Apply -> target={currentTarget.ModeName}, " +
                    $"hold={currentHoldSeconds:0.00}, strong={currentStrongThreshold:0.00}, weak={currentWeakThreshold:0.00}",
                    this
                );
            }
        }

        public void RegisterTaskResult(AdaptiveTaskResult result)
        {
            if (result == null) return;

            recentResults.Add(result);
            if (recentResults.Count > recentWindowSize)
                recentResults.RemoveAt(0);

            totalResultsStored = recentResults.Count;

            if (!adaptiveEnabled)
            {
                lastDecision = "Adaptive disabled";
                return;
            }

            List<AdaptiveTaskResult> valid = GetValidRecentResults();
            validResultsInWindow = valid.Count;

            if (valid.Count > 0)
            {
                lastWindowAvgScore = AverageScore(valid);
                lastWindowAvgStrongRatio = AverageStrongRatio(valid);
            }
            else
            {
                lastWindowAvgScore = 0f;
                lastWindowAvgStrongRatio = 0f;
            }

            if (valid.Count < minValidTasksBeforeAdjust)
            {
                lastDecision = $"Not enough valid tasks ({valid.Count}/{minValidTasksBeforeAdjust})";
                return;
            }

            if (cooldownRemaining > 0)
            {
                cooldownRemaining--;
                lastDecision = $"Cooldown ({cooldownRemaining} left)";
                return;
            }

            bool shouldHarder = ShouldMakeHarder(valid);
            bool shouldEasier = ShouldMakeEasier(valid);

            if (shouldHarder && !shouldEasier)
            {
                MakeHarder();
                ApplyToCurrentTarget();
                cooldownRemaining = cooldownTasksAfterAdjustment;
                totalAdjustments++;
                harderCount++;
                lastDecision = "Harder";
            }
            else if (shouldEasier && !shouldHarder)
            {
                MakeEasier();
                ApplyToCurrentTarget();
                cooldownRemaining = cooldownTasksAfterAdjustment;
                totalAdjustments++;
                easierCount++;
                lastDecision = "Easier";
            }
            else
            {
                lastDecision = "Stay";
            }

            if (debugLogs)
            {
                Debug.Log(
                    $"[GlobalAdaptive] decision={lastDecision}, valid={valid.Count}, " +
                    $"avgScore={lastWindowAvgScore:0.0}, avgStrong={lastWindowAvgStrongRatio:0.00}, " +
                    $"hold={currentHoldSeconds:0.00}, strong={currentStrongThreshold:0.00}, weak={currentWeakThreshold:0.00}",
                    this
                );
            }
        }

        private List<AdaptiveTaskResult> GetValidRecentResults()
        {
            List<AdaptiveTaskResult> valid = new List<AdaptiveTaskResult>();

            for (int i = 0; i < recentResults.Count; i++)
            {
                if (recentResults[i].trackingRatio >= validTrackingRatioThreshold)
                    valid.Add(recentResults[i]);
            }

            return valid;
        }

        private bool ShouldMakeHarder(List<AdaptiveTaskResult> valid)
        {
            List<AdaptiveTaskResult> last3 = GetLast(valid, 3);
            List<AdaptiveTaskResult> last2 = GetLast(valid, 2);

            bool ruleA =
                last3.Count == 3 &&
                AllSuccess(last3) &&
                AverageScore(last3) >= levelUpAvgScore3 &&
                AverageStrongRatio(last3) >= levelUpAvgStrongRatio3;

            bool ruleB =
                last2.Count == 2 &&
                AllSuccess(last2) &&
                AverageScore(last2) >= levelUpAvgScore2 &&
                AverageStrongRatio(last2) >= levelUpAvgStrongRatio2;

            return ruleA || ruleB;
        }

        private bool ShouldMakeEasier(List<AdaptiveTaskResult> valid)
        {
            List<AdaptiveTaskResult> last3 = GetLast(valid, 3);
            List<AdaptiveTaskResult> last2 = GetLast(valid, 2);

            int failCount = 0;
            int trackingLostCount = 0;

            for (int i = 0; i < last3.Count; i++)
            {
                if (!last3[i].success) failCount++;
                if (last3[i].errorType == AdaptiveErrorType.TrackingLost) trackingLostCount++;
            }

            bool ruleA =
                last3.Count == 3 &&
                failCount >= 2 &&
                trackingLostCount < failCount;

            bool ruleB =
                last3.Count == 3 &&
                AverageScore(last3) < levelDownAvgScore3;

            bool ruleC =
                last2.Count == 2 &&
                last2[0].errorType == AdaptiveErrorType.UnstableHold &&
                last2[1].errorType == AdaptiveErrorType.UnstableHold;

            return ruleA || ruleB || ruleC;
        }

        private void MakeHarder()
        {
            currentHoldSeconds += holdSecondsStep;
            currentStrongThreshold += strongThresholdStep;
            currentWeakThreshold += weakThresholdStep;
            ClampCurrentParams();
        }

        private void MakeEasier()
        {
            currentHoldSeconds -= holdSecondsStep;
            currentStrongThreshold -= strongThresholdStep;
            currentWeakThreshold -= weakThresholdStep;
            ClampCurrentParams();
        }

        private void ClampCurrentParams()
        {
            currentHoldSeconds = Mathf.Clamp(currentHoldSeconds, minHoldSeconds, maxHoldSeconds);
            currentStrongThreshold = Mathf.Clamp(currentStrongThreshold, minStrongThreshold, maxStrongThreshold);
            currentWeakThreshold = Mathf.Clamp(currentWeakThreshold, minWeakThreshold, maxWeakThreshold);

            if (currentWeakThreshold > currentStrongThreshold - minGapBetweenStrongAndWeak)
            {
                currentWeakThreshold = currentStrongThreshold - minGapBetweenStrongAndWeak;
            }

            currentWeakThreshold = Mathf.Clamp(currentWeakThreshold, minWeakThreshold, maxWeakThreshold);
        }

        private List<AdaptiveTaskResult> GetLast(List<AdaptiveTaskResult> src, int n)
        {
            List<AdaptiveTaskResult> result = new List<AdaptiveTaskResult>();
            if (src == null || src.Count == 0 || n <= 0) return result;

            int start = Mathf.Max(0, src.Count - n);
            for (int i = start; i < src.Count; i++)
                result.Add(src[i]);

            return result;
        }

        private bool AllSuccess(List<AdaptiveTaskResult> list)
        {
            if (list == null || list.Count == 0) return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (!list[i].success) return false;
            }

            return true;
        }

        private float AverageScore(List<AdaptiveTaskResult> list)
        {
            if (list == null || list.Count == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < list.Count; i++)
                sum += list[i].score;

            return sum / list.Count;
        }

        private float AverageStrongRatio(List<AdaptiveTaskResult> list)
        {
            if (list == null || list.Count == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < list.Count; i++)
                sum += list[i].strongMatchRatio;

            return sum / list.Count;
        }
    }
}