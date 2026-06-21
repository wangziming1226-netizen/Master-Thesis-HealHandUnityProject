using System;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Thesis.Core;

namespace Thesis.Modules.Assessment
{
    public enum AssessmentFeedbackState
    {
        Idle,
        TrackingLost,
        Ready,
        Holding,
        Success,
        Failed
    }

    public enum AssessmentErrorType
    {
        None,
        TrackingLost,
        GestureMismatch,
        UnstableHold,
        Timeout
    }

    public enum AssessmentGestureType
    {
        OpenHand,
        Fist,
        OK
    }

    public class AssessmentModeController : MonoBehaviour
    {
        [Header("Input")]
        public ThesisGestureInput gestureInput;
        public HandGestureRecognizer gestureRecognizer;

        [Header("Target")]
        public AssessmentGestureType targetGestureType = AssessmentGestureType.OpenHand;
        public float targetHoldSeconds = 3.0f;
        public float maxTaskSeconds = 10.0f;
        public int maxAttempts = 3;

        [Header("Rehabilitation-Friendly Matching Thresholds")]
        [Range(0f, 1f)] public float strongMatchThreshold = 0.80f;
        [Range(0f, 1f)] public float weakMatchThreshold = 0.45f;
        [Range(0f, 1f)] public float strongMatchMargin = 0.12f;
        [Range(0f, 1f)] public float weakMatchProgressMultiplier = 0.35f;

        [Header("OK Gesture Relaxation (Aligned with Practice)")]
        public bool relaxOKThresholds = true;
        [Range(0f, 1f)] public float okStrongMatchThreshold = 0.20f;
        [Range(0f, 1f)] public float okWeakMatchThreshold = 0.00f;

        [Header("Adaptive Difficulty")]
        public bool adaptiveEnabled = false;
        public int baseDifficultyLevel = 1;
        public int currentDifficultyLevel = 1;
        public int initialDifficultyLevel = 1;
        public int startDifficultyLevel = 1;
        public int scannedDifficultyLevel = 1;

        public string lastScannedCardId = "";
        public string lastScannedGesture = "";
        public int lastScannedDifficulty = 1;

        [Header("Adaptive Session Runtime")]
        public bool adaptiveSessionSeeded = false;
        public int adaptiveSeedDifficultyLevel = 1;
        public string adaptiveSeedGestureKey = "";

        private int adaptiveSessionDifficultyLevel = 1;
        private float adaptiveSessionHoldSeconds = 3.0f;
        private float adaptiveSessionStrongThreshold = 0.80f;
        private float adaptiveSessionWeakThreshold = 0.45f;

        [Tooltip("Hold-time increase per adjustment")]
        public float adaptiveHoldStep = 0.25f;
        [Tooltip("Strong-threshold increase per adjustment")]
        public float adaptiveStrongStep = 0.02f;
        [Tooltip("Weak-threshold increase per adjustment")]
        public float adaptiveWeakStep = 0.02f;

        [Header("Adaptive Guardrails")]
        public int minDifficultyLevel = 1;
        public int maxDifficultyLevel = 4;
        public float minHoldSeconds = 2.0f;
        public float maxHoldSeconds = 5.0f;
        public float minStrongThreshold = 0.55f;
        public float maxStrongThreshold = 0.90f;
        public float minWeakThreshold = 0.30f;
        public float maxWeakThreshold = 0.70f;

        [Header("Mode UI (optional)")]
        public TMP_Text txtModeValue;
        public TMP_Text txtDifficultyValue;

        [Header("Right Panel UI")]
        public TMP_Text txtStateValue;
        public TMP_Text txtTimerValue;
        public TMP_Text txtTrackingValue;
        public TMP_Text txtScoreValue;
        public TMP_Text txtAttemptValue;

        [Header("Center UI")]
        public TMP_Text txtInstructionValue;
        public TMP_Text txtGestureTargetValue;
        public TMP_Text txtSubHintValue;

        [Header("Left Panel UI (optional)")]
        public TMP_Text txtTestNameValue;
        public TMP_Text txtTargetGestureValue;
        public TMP_Text txtDurationValue;
        public TMP_Text txtAttemptsInfoValue;

        [Header("Feedback UI (optional)")]
        public TMP_Text txtPrimaryMessage;
        public TMP_Text txtSecondaryMessage;
        public Image progressFill;

        [Header("Buttons")]
        public Button btnStart;
        public Button btnPause;
        public Button btnReset;

        [Header("Log Export")]
        public bool exportLogs = true;
        public string sessionId = "assessment_smoke_001";
        public string logFileName = "assessment_log.csv";

        [Header("Debug")]
        public bool debugLogs = false;

        [Header("External Adaptive Control")]
        [Tooltip("When enabled, an external adaptive controller manages difficulty and this module will not adjust it after export.")]
        public bool useExternalAdaptiveController = false;

        private enum RunState
        {
            Idle,
            Running,
            Paused,
            Success,
            Failed
        }

        private RunState state = RunState.Idle;

        private float holdAccum = 0f;
        private float assistAccum = 0f;
        private float elapsedTime = 0f;
        private float lostTrackingAccum = 0f;

        private int currentAttempt = 1;
        private bool hasExportedCurrentResult = false;
        private bool hasAppliedAdaptiveForCurrentResult = false;
        private float debugNextTime = 0f;

        private bool baseDifficultySnapshotCaptured = false;
        private float baseHoldSecondsSnapshot = 3.0f;
        private float baseStrongThresholdSnapshot = 0.80f;
        private float baseWeakThresholdSnapshot = 0.45f;

        private float targetConfidence = 0f;
        private float secondBestConfidence = 0f;
        private bool isStrongMatch = false;
        private bool isWeakMatch = false;

        [Header("Unified Output")]
        public AssessmentFeedbackState feedbackState = AssessmentFeedbackState.Idle;
        public AssessmentErrorType errorType = AssessmentErrorType.None;

        [TextArea] public string primaryMessage = "Ready to start";
        [TextArea] public string secondaryMessage = "Show your hand to begin";

        [Range(0f, 1f)] public float progress01 = 0f;
        public bool isCompleted = false;
        public bool isSuccess = false;
        public float completionTime = 0f;

        [Header("Scoring Output")]
        [Range(0f, 100f)] public float score = 0f;
        [Range(0f, 1f)] public float holdRatio = 0f;
        [Range(0f, 1f)] public float assistRatio = 0f;
        [Range(0f, 1f)] public float trackingRatio = 1f;

        public float holdScore = 0f;
        public float trackingScore = 0f;
        public float assistScore = 0f;
        public float penaltyScore = 0f;

        [Header("Confidence Debug")]
        [Range(0f, 1f)] public float debugOpenConfidence = 0f;
        [Range(0f, 1f)] public float debugFistConfidence = 0f;
        [Range(0f, 1f)] public float debugOKConfidence = 0f;

        private string TargetGestureKey => GetGestureKey(targetGestureType);
        private string TargetGestureDisplay => GetGestureDisplayName(targetGestureType);

        void Awake()
        {
#if UNITY_2023_1_OR_NEWER
            if (gestureInput == null)
            {
                var all = FindObjectsByType<ThesisGestureInput>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (all != null && all.Length > 0) gestureInput = all[0];
            }

            if (gestureRecognizer == null)
            {
                var allRec = FindObjectsByType<HandGestureRecognizer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (allRec != null && allRec.Length > 0) gestureRecognizer = allRec[0];
            }
#else
            if (gestureInput == null) gestureInput = FindObjectOfType<ThesisGestureInput>();
            if (gestureRecognizer == null) gestureRecognizer = FindObjectOfType<HandGestureRecognizer>();
#endif

            if (btnStart != null) btnStart.onClick.AddListener(OnStart);
            if (btnPause != null) btnPause.onClick.AddListener(OnPause);
            if (btnReset != null) btnReset.onClick.AddListener(OnReset);

            CaptureBaseDifficultySnapshot();
            ResetToSeedDifficulty();

            InitTexts();
            RefreshUnifiedOutput(false, "none");
            RecalculateScore();
            RefreshUI(false);
            RefreshModeAndDifficultyUI();

            if (debugLogs)
            {
                Debug.Log($"[Assessment] Awake on {name} (scene={gameObject.scene.name})");
                Debug.Log(gestureInput != null
                    ? $"[Assessment] gestureInput bound = {gestureInput.name}"
                    : "[Assessment] gestureInput is NULL");
                Debug.Log(gestureRecognizer != null
                    ? $"[Assessment] gestureRecognizer bound = {gestureRecognizer.name}"
                    : "[Assessment] gestureRecognizer is NULL");
            }
        }

        void Update()
        {
            bool tracking = gestureInput != null && gestureInput.IsTracking;
            string gesture = gestureInput != null ? gestureInput.currentGesture : "none";

            UpdateConfidenceCache(gesture);

            if (state == RunState.Running)
            {
                elapsedTime += Time.deltaTime;

                if (!tracking)
                    lostTrackingAccum += Time.deltaTime;

                if (tracking)
                {
                    if (isStrongMatch)
                    {
                        holdAccum += Time.deltaTime;
                    }
                    else if (isWeakMatch)
                    {
                        assistAccum += Time.deltaTime * weakMatchProgressMultiplier;
                    }

                    if (holdAccum > targetHoldSeconds)
                        holdAccum = targetHoldSeconds;

                    if (assistAccum > targetHoldSeconds)
                        assistAccum = targetHoldSeconds;

                    if (holdAccum >= targetHoldSeconds)
                    {
                        state = RunState.Success;
                        isCompleted = true;
                        isSuccess = true;
                        completionTime = elapsedTime;
                        errorType = AssessmentErrorType.None;
                    }
                }

                if (elapsedTime >= maxTaskSeconds && state == RunState.Running)
                {
                    state = RunState.Failed;
                    isCompleted = true;
                    isSuccess = false;
                    completionTime = elapsedTime;

                    if (!tracking)
                        errorType = AssessmentErrorType.TrackingLost;
                    else if (holdAccum > 0f || assistAccum > 0f)
                        errorType = AssessmentErrorType.UnstableHold;
                    else
                        errorType = AssessmentErrorType.GestureMismatch;

                    if (currentAttempt < maxAttempts)
                        currentAttempt++;
                }
            }

            RefreshUnifiedOutput(tracking, gesture);
            RecalculateScore();
            RefreshUI(tracking);
            TryExportResultIfNeeded();
            RefreshModeAndDifficultyUI();

            if (debugLogs && Time.time >= debugNextTime)
            {
                debugNextTime = Time.time + 0.5f;
                Debug.Log(
                    $"[Assessment] target={TargetGestureKey}, state={state}, tracking={tracking}, gesture={gesture}, " +
                    $"targetConf={targetConfidence:0.00}, secondConf={secondBestConfidence:0.00}, " +
                    $"strong={isStrongMatch}, weak={isWeakMatch}, " +
                    $"hold={holdAccum:0.00}/{targetHoldSeconds:0.00}, assist={assistAccum:0.00}/{targetHoldSeconds:0.00}, " +
                    $"elapsed={elapsedTime:0.00}/{maxTaskSeconds:0.00}, score={score:0.0}, " +
                    $"adaptive={adaptiveEnabled}, level={currentDifficultyLevel}"
                );
            }
        }

        private void UpdateConfidenceCache(string currentGesture)
        {
            if (gestureRecognizer == null)
            {
                targetConfidence = 0f;
                secondBestConfidence = 0f;
                isStrongMatch = false;
                isWeakMatch = false;
                debugOpenConfidence = 0f;
                debugFistConfidence = 0f;
                debugOKConfidence = 0f;
                return;
            }

            debugOpenConfidence = gestureRecognizer.OpenConfidence;
            debugFistConfidence = gestureRecognizer.FistConfidence;
            debugOKConfidence = gestureRecognizer.OKConfidence;

            float openC = gestureRecognizer.OpenConfidence;
            float fistC = gestureRecognizer.FistConfidence;
            float okC = gestureRecognizer.OKConfidence;

            switch (targetGestureType)
            {
                case AssessmentGestureType.Fist:
                    targetConfidence = fistC;
                    secondBestConfidence = Mathf.Max(openC, okC);
                    break;

                case AssessmentGestureType.OK:
                    targetConfidence = okC;
                    secondBestConfidence = Mathf.Max(openC, fistC);
                    break;

                case AssessmentGestureType.OpenHand:
                default:
                    targetConfidence = openC;
                    secondBestConfidence = Mathf.Max(fistC, okC);
                    break;
            }

            bool labelMatches = currentGesture == TargetGestureKey;

            // Aligned with Practice: OK matching does not require a confidence margin before hold accumulation begins.
            if (relaxOKThresholds && targetGestureType == AssessmentGestureType.OK)
            {
                isStrongMatch = labelMatches && targetConfidence >= okStrongMatchThreshold;
                isWeakMatch = !isStrongMatch && labelMatches && targetConfidence >= okWeakMatchThreshold;
                return;
            }

            bool strongByLabel =
                labelMatches &&
                targetConfidence >= strongMatchThreshold;

            bool strongByConfidence =
                targetConfidence >= strongMatchThreshold &&
                (targetConfidence - secondBestConfidence) >= strongMatchMargin;

            isStrongMatch = strongByLabel || strongByConfidence;
            isWeakMatch = !isStrongMatch && targetConfidence >= weakMatchThreshold;
        }

        private void InitTexts()
        {
            if (txtInstructionValue != null)
                txtInstructionValue.text = $"Perform {TargetGestureDisplay} and try to hold it steadily.";

            if (txtGestureTargetValue != null)
                txtGestureTargetValue.text = TargetGestureDisplay;

            if (txtSubHintValue != null)
                txtSubHintValue.text = "Keep your hand visible in the tracking area.";

            if (txtTestNameValue != null)
                txtTestNameValue.text = "Gesture Stability Test";

            if (txtTargetGestureValue != null)
                txtTargetGestureValue.text = TargetGestureDisplay;

            if (txtDurationValue != null)
                txtDurationValue.text = $"{targetHoldSeconds:0.#} seconds";

            if (txtAttemptsInfoValue != null)
                txtAttemptsInfoValue.text = $"{currentAttempt} / {maxAttempts}";
        }

        private void RefreshUnifiedOutput(bool tracking, string gesture)
        {
            float visualAccum = Mathf.Max(holdAccum, assistAccum);
            progress01 = targetHoldSeconds > 0f ? Mathf.Clamp01(visualAccum / targetHoldSeconds) : 0f;

            switch (state)
            {
                case RunState.Idle:
                    feedbackState = AssessmentFeedbackState.Idle;
                    errorType = AssessmentErrorType.None;
                    primaryMessage = "Ready to start";
                    secondaryMessage = $"Show {TargetGestureDisplay} to begin";
                    isCompleted = false;
                    isSuccess = false;
                    break;

                case RunState.Paused:
                    feedbackState = AssessmentFeedbackState.Idle;
                    primaryMessage = "Paused";
                    secondaryMessage = "Press Start to continue";
                    break;

                case RunState.Success:
                    feedbackState = AssessmentFeedbackState.Success;
                    primaryMessage = "Success";
                    secondaryMessage = $"You completed {TargetGestureDisplay}";
                    break;

                case RunState.Failed:
                    feedbackState = AssessmentFeedbackState.Failed;

                    switch (errorType)
                    {
                        case AssessmentErrorType.TrackingLost:
                            primaryMessage = "Tracking lost";
                            secondaryMessage = "Please move your hand into view";
                            break;

                        case AssessmentErrorType.GestureMismatch:
                            primaryMessage = "Try again";
                            secondaryMessage = $"Keep moving toward {TargetGestureDisplay}";
                            break;

                        case AssessmentErrorType.UnstableHold:
                            primaryMessage = "Almost there";
                            secondaryMessage = "Good attempt. Try to hold more steadily";
                            break;

                        case AssessmentErrorType.Timeout:
                        default:
                            primaryMessage = "Time out";
                            secondaryMessage = "Please try again";
                            break;
                    }
                    break;

                case RunState.Running:
                    if (!tracking)
                    {
                        feedbackState = AssessmentFeedbackState.TrackingLost;
                        primaryMessage = "Hand not detected";
                        secondaryMessage = "Please move your hand into view";
                    }
                    else if (isStrongMatch)
                    {
                        feedbackState = AssessmentFeedbackState.Holding;
                        primaryMessage = "Hold steady";
                        secondaryMessage = $"Good match. Keep {TargetGestureDisplay} for {targetHoldSeconds:0.0}s";
                    }
                    else if (isWeakMatch)
                    {
                        feedbackState = AssessmentFeedbackState.Holding;
                        primaryMessage = "Almost there";
                        secondaryMessage = $"Good attempt. Keep moving toward {TargetGestureDisplay}";
                    }
                    else
                    {
                        feedbackState = AssessmentFeedbackState.Ready;
                        primaryMessage = "Make the target gesture";
                        secondaryMessage = $"Target: {TargetGestureDisplay}";
                    }
                    break;
            }
        }

        private void RecalculateScore()
        {
            holdRatio = targetHoldSeconds > 0f
                ? Mathf.Clamp01(holdAccum / targetHoldSeconds)
                : 0f;

            assistRatio = targetHoldSeconds > 0f
                ? Mathf.Clamp01(assistAccum / targetHoldSeconds)
                : 0f;

            trackingRatio = maxTaskSeconds > 0f
                ? Mathf.Clamp01(1f - (lostTrackingAccum / maxTaskSeconds))
                : 1f;

            holdScore = holdRatio * 25f;
            trackingScore = trackingRatio * 15f;
            assistScore = assistRatio * 8f;
            penaltyScore = GetPenalty(errorType);

            float baseScore = isSuccess ? 50f : 20f;
            float effortBonus = isSuccess ? 0f : targetConfidence * 6f;

            float rawScore = baseScore + holdScore + trackingScore + assistScore + effortBonus - penaltyScore;
            score = Mathf.Clamp(rawScore, 0f, 100f);
        }

        private float GetPenalty(AssessmentErrorType type)
        {
            switch (type)
            {
                case AssessmentErrorType.TrackingLost:
                    return 5f;
                case AssessmentErrorType.GestureMismatch:
                    return 6f;
                case AssessmentErrorType.UnstableHold:
                    return 4f;
                case AssessmentErrorType.Timeout:
                    return 3f;
                case AssessmentErrorType.None:
                default:
                    return 0f;
            }
        }

        private void RefreshUI(bool tracking)
        {
            if (txtTrackingValue != null)
                txtTrackingValue.text = tracking ? "Present" : "Lost";

            if (txtStateValue != null)
                txtStateValue.text = state.ToString();

            if (txtTimerValue != null)
                txtTimerValue.text = $"{holdAccum:0.0} / {targetHoldSeconds:0.0}";

            if (txtScoreValue != null)
                txtScoreValue.text = $"{score:0}";

            if (txtAttemptValue != null)
                txtAttemptValue.text = $"{currentAttempt} / {maxAttempts}";

            if (txtAttemptsInfoValue != null)
                txtAttemptsInfoValue.text = $"{currentAttempt} / {maxAttempts}";

            if (txtPrimaryMessage != null)
                txtPrimaryMessage.text = primaryMessage;

            if (txtSecondaryMessage != null)
                txtSecondaryMessage.text = secondaryMessage;

            if (progressFill != null)
                progressFill.fillAmount = progress01;

            if (btnStart != null)
                btnStart.interactable = (state == RunState.Idle || state == RunState.Paused);

            if (btnPause != null)
                btnPause.interactable = (state == RunState.Running);

            if (btnReset != null)
                btnReset.interactable = true;
        }

        private void RefreshModeAndDifficultyUI()
        {
            if (txtModeValue != null)
                txtModeValue.text = adaptiveEnabled ? "Adaptive" : "Fixed";

            if (txtDifficultyValue != null)
                txtDifficultyValue.text =
                    $"L={currentDifficultyLevel} H={targetHoldSeconds:0.00} S={strongMatchThreshold:0.00} W={weakMatchThreshold:0.00}";
        }

        private void TryExportResultIfNeeded()
        {
            if (!exportLogs) return;
            if (!isCompleted) return;
            if (hasExportedCurrentResult) return;

            ExportAssessmentLog();

            if (!useExternalAdaptiveController)
                ApplyAdaptiveAdjustmentFromResult();

            hasExportedCurrentResult = true;
        }

        private void ApplyAdaptiveAdjustmentFromResult()
        {
            if (!adaptiveEnabled) return;
            if (hasAppliedAdaptiveForCurrentResult) return;

            bool harder =
                isSuccess &&
                holdRatio >= 1f &&
                trackingRatio >= 0.85f &&
                score >= 75f;

            if (harder && currentDifficultyLevel < maxDifficultyLevel)
            {
                currentDifficultyLevel++;
                targetHoldSeconds = Mathf.Clamp(targetHoldSeconds + adaptiveHoldStep, minHoldSeconds, maxHoldSeconds);
                strongMatchThreshold = Mathf.Clamp(strongMatchThreshold + adaptiveStrongStep, minStrongThreshold, maxStrongThreshold);
                weakMatchThreshold = Mathf.Clamp(weakMatchThreshold + adaptiveWeakStep, minWeakThreshold, maxWeakThreshold);
            }
            if (adaptiveEnabled)
            {
                SyncAdaptiveSessionFromCurrent();
                adaptiveSessionSeeded = true;
                adaptiveSeedDifficultyLevel = Mathf.Clamp(adaptiveSeedDifficultyLevel, minDifficultyLevel, maxDifficultyLevel);
                adaptiveSeedGestureKey = TargetGestureKey;
            }

            hasAppliedAdaptiveForCurrentResult = true;
            RefreshModeAndDifficultyUI();
        }

        private void ExportAssessmentLog()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "AssessmentLogs");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, logFileName);
                bool fileExists = File.Exists(filePath);

                using (var sw = new StreamWriter(filePath, true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine(
                            "session_id,timestamp_utc,module,difficulty_mode,difficulty_level,scanned_card_id,target_gesture,attempt_index,max_attempts,success,score,error_type,feedback_state," +
                            "completion_time_sec,target_hold_sec,hold_ratio,assist_ratio,tracking_ratio,target_confidence," +
                            "open_confidence,fist_confidence,ok_confidence,strong_match_threshold,weak_match_threshold,strong_match_margin"
                        );
                    }

                    string line =
                        EscapeCsv(sessionId) + "," +
                        EscapeCsv(DateTime.UtcNow.ToString("o")) + "," +
                        EscapeCsv("Assessment") + "," +
                        EscapeCsv(GetDifficultyMode()) + "," +
                        currentDifficultyLevel.ToString() + "," +
                        EscapeCsv(lastScannedCardId) + "," +
                        EscapeCsv(TargetGestureDisplay) + "," +
                        currentAttempt.ToString() + "," +
                        maxAttempts.ToString() + "," +
                        isSuccess.ToString().ToLowerInvariant() + "," +
                        score.ToString("0.0") + "," +
                        EscapeCsv(errorType.ToString()) + "," +
                        EscapeCsv(feedbackState.ToString()) + "," +
                        completionTime.ToString("0.00") + "," +
                        targetHoldSeconds.ToString("0.00") + "," +
                        holdRatio.ToString("0.000") + "," +
                        assistRatio.ToString("0.000") + "," +
                        trackingRatio.ToString("0.000") + "," +
                        targetConfidence.ToString("0.000") + "," +
                        debugOpenConfidence.ToString("0.000") + "," +
                        debugFistConfidence.ToString("0.000") + "," +
                        debugOKConfidence.ToString("0.000") + "," +
                        strongMatchThreshold.ToString("0.000") + "," +
                        weakMatchThreshold.ToString("0.000") + "," +
                        strongMatchMargin.ToString("0.000");

                    sw.WriteLine(line);
                }

                if (debugLogs)
                    Debug.Log($"[Assessment] Log exported -> {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Assessment] ExportAssessmentLog failed: {ex.Message}");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        public void SetAdaptiveMode(bool enabled)
        {
            adaptiveEnabled = enabled;

            if (!adaptiveEnabled)
            {
                adaptiveSessionSeeded = false;
                ResetToSeedDifficulty();
            }
            else
            {
                if (ShouldReseedAdaptiveSession())
                    SeedAdaptiveSessionFromCurrentScan();
                else
                    ApplyAdaptiveSessionSnapshot();
            }

            RefreshModeAndDifficultyUI();
        }

        public void OnClickFixed()
        {
            adaptiveEnabled = false;
            adaptiveSessionSeeded = false;

            ResetRuntimeStateOnly();
            ResetToSeedDifficulty();
            state = RunState.Idle;
            currentAttempt = 1;
            InitTexts();
            RefreshUnifiedOutput(gestureInput != null && gestureInput.IsTracking, gestureInput != null ? gestureInput.currentGesture : "none");
            RecalculateScore();
            RefreshUI(gestureInput != null && gestureInput.IsTracking);
            RefreshModeAndDifficultyUI();
        }

        public void OnClickAdaptive()
        {
            adaptiveEnabled = true;
            adaptiveSessionSeeded = false;

            ResetRuntimeStateOnly();
            SeedAdaptiveSessionFromCurrentScan();
            state = RunState.Idle;
            currentAttempt = 1;
            InitTexts();
            RefreshUnifiedOutput(gestureInput != null && gestureInput.IsTracking, gestureInput != null ? gestureInput.currentGesture : "none");
            RecalculateScore();
            RefreshUI(gestureInput != null && gestureInput.IsTracking);
            RefreshModeAndDifficultyUI();
        }

        private bool ShouldReseedAdaptiveSession()
        {
            int seedLevel = ResolveSeedDifficultyLevel();
            string gestureKey = TargetGestureKey;

            return !adaptiveSessionSeeded ||
                   adaptiveSeedDifficultyLevel != seedLevel ||
                   adaptiveSeedGestureKey != gestureKey;
        }

        private void SeedAdaptiveSessionFromCurrentScan()
        {
            int seedLevel = ResolveSeedDifficultyLevel();
            ApplyDifficultyLevel(seedLevel);

            adaptiveSeedDifficultyLevel = seedLevel;
            adaptiveSeedGestureKey = TargetGestureKey;
            adaptiveSessionSeeded = true;

            SyncAdaptiveSessionFromCurrent();
            hasAppliedAdaptiveForCurrentResult = false;
        }

        private void SyncAdaptiveSessionFromCurrent()
        {
            adaptiveSessionDifficultyLevel = currentDifficultyLevel;
            adaptiveSessionHoldSeconds = targetHoldSeconds;
            adaptiveSessionStrongThreshold = strongMatchThreshold;
            adaptiveSessionWeakThreshold = weakMatchThreshold;
        }

        private void ApplyAdaptiveSessionSnapshot()
        {
            currentDifficultyLevel = Mathf.Clamp(adaptiveSessionDifficultyLevel, minDifficultyLevel, maxDifficultyLevel);
            targetHoldSeconds = Mathf.Clamp(adaptiveSessionHoldSeconds, minHoldSeconds, maxHoldSeconds);
            strongMatchThreshold = Mathf.Clamp(adaptiveSessionStrongThreshold, minStrongThreshold, maxStrongThreshold);
            weakMatchThreshold = Mathf.Clamp(adaptiveSessionWeakThreshold, minWeakThreshold, maxWeakThreshold);

            EnforceThresholdGap();

hasAppliedAdaptiveForCurrentResult = false;
        }

        private void EnforceThresholdGap()
        {
            weakMatchThreshold = Mathf.Min(weakMatchThreshold, strongMatchThreshold - 0.10f);
            weakMatchThreshold = Mathf.Clamp(weakMatchThreshold, minWeakThreshold, maxWeakThreshold);
        }

        private void CaptureBaseDifficultySnapshot()
        {
            if (baseDifficultySnapshotCaptured) return;

            baseHoldSecondsSnapshot = targetHoldSeconds;
            baseStrongThresholdSnapshot = strongMatchThreshold;
            baseWeakThresholdSnapshot = weakMatchThreshold;
            baseDifficultySnapshotCaptured = true;
        }

        private int ResolveSeedDifficultyLevel()
        {
            int seedLevel =
                scannedDifficultyLevel > 0 ? scannedDifficultyLevel :
                startDifficultyLevel > 0 ? startDifficultyLevel :
                initialDifficultyLevel > 0 ? initialDifficultyLevel :
                baseDifficultyLevel;

            return Mathf.Clamp(seedLevel, minDifficultyLevel, maxDifficultyLevel);
        }

        private void ApplyDifficultyLevel(int level)
        {
            CaptureBaseDifficultySnapshot();

            int clampedLevel = Mathf.Clamp(level, minDifficultyLevel, maxDifficultyLevel);
            int baseLevel = Mathf.Clamp(baseDifficultyLevel, minDifficultyLevel, maxDifficultyLevel);
            int delta = clampedLevel - baseLevel;

            currentDifficultyLevel = clampedLevel;
            targetHoldSeconds = Mathf.Clamp(baseHoldSecondsSnapshot + delta * adaptiveHoldStep, minHoldSeconds, maxHoldSeconds);
            strongMatchThreshold = Mathf.Clamp(baseStrongThresholdSnapshot + delta * adaptiveStrongStep, minStrongThreshold, maxStrongThreshold);
            weakMatchThreshold = Mathf.Clamp(baseWeakThresholdSnapshot + delta * adaptiveWeakStep, minWeakThreshold, maxWeakThreshold);

            EnforceThresholdGap();

}

        private void ResetToSeedDifficulty()
        {
            ApplyDifficultyLevel(ResolveSeedDifficultyLevel());
            hasAppliedAdaptiveForCurrentResult = false;

            if (adaptiveEnabled)
                SyncAdaptiveSessionFromCurrent();
        }

        private void OnStart()
        {
            if (state == RunState.Success) return;

            if (state == RunState.Failed)
                ResetRuntimeStateOnly();

            InitTexts();
            state = RunState.Running;
        }

        private void OnPause()
        {
            if (state == RunState.Running)
                state = RunState.Paused;
        }

        private void OnReset()
        {
            ResetRuntimeStateOnly();

            if (adaptiveEnabled)
            {
                if (ShouldReseedAdaptiveSession())
                    SeedAdaptiveSessionFromCurrentScan();
                else
                    ApplyAdaptiveSessionSnapshot();
            }
            else
            {
                adaptiveSessionSeeded = false;
                ResetToSeedDifficulty();
            }

            state = RunState.Idle;
            currentAttempt = 1;
            InitTexts();
            RefreshUnifiedOutput(gestureInput != null && gestureInput.IsTracking, gestureInput != null ? gestureInput.currentGesture : "none");
            RecalculateScore();
            RefreshUI(gestureInput != null && gestureInput.IsTracking);
            RefreshModeAndDifficultyUI();
        }

        private void ResetRuntimeStateOnly()
        {
            holdAccum = 0f;
            assistAccum = 0f;
            elapsedTime = 0f;
            lostTrackingAccum = 0f;

            progress01 = 0f;
            isCompleted = false;
            isSuccess = false;
            completionTime = 0f;
            errorType = AssessmentErrorType.None;
            score = 0f;
            holdRatio = 0f;
            assistRatio = 0f;
            trackingRatio = 1f;
            holdScore = 0f;
            trackingScore = 0f;
            assistScore = 0f;
            penaltyScore = 0f;
            targetConfidence = 0f;
            secondBestConfidence = 0f;
            isStrongMatch = false;
            isWeakMatch = false;
            hasExportedCurrentResult = false;
            hasAppliedAdaptiveForCurrentResult = false;
        }

        private string GetDifficultyMode() => adaptiveEnabled ? "adaptive" : "fixed";

        private string GetGestureKey(AssessmentGestureType type)
        {
            switch (type)
            {
                case AssessmentGestureType.Fist:
                    return "fist";
                case AssessmentGestureType.OK:
                    return "ok";
                case AssessmentGestureType.OpenHand:
                default:
                    return "open_hand";
            }
        }

        private string GetGestureDisplayName(AssessmentGestureType type)
        {
            switch (type)
            {
                case AssessmentGestureType.Fist:
                    return "Fist";
                case AssessmentGestureType.OK:
                    return "OK";
                case AssessmentGestureType.OpenHand:
                default:
                    return "Open Hand";
            }
        }
    }
}