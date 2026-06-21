
using System;
using System.IO;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Thesis.Core;

namespace Thesis.Modules.Practice
{
    public enum TrainingFeedbackState
    {
        Idle,
        TrackingLost,
        Ready,
        Holding,
        Success,
        Failed
    }

    public enum TrainingErrorType
    {
        None,
        TrackingLost,
        GestureMismatch,
        UnstableHold,
        Timeout
    }

    public enum PracticeGestureType
    {
        OpenHand,
        Fist,
        OK,
        ThumbsUp
    }

    public class PracticeModeController : MonoBehaviour
    {
        [Header("Input")]
        public ThesisGestureInput gestureInput;
        public HandGestureRecognizer gestureRecognizer;

        [Header("Runtime Switch UI")]
        public Button btnFixedMode;
        public Button btnAdaptiveMode;
        public Button btnGestureOpenHand;
        public Button btnGestureFist;
        public Button btnGestureOK;

        public TMP_Text txtModeValue;
        public TMP_Text txtDifficultyValue;

        [Header("Baseline Difficulty")]
        public float baselineHoldSeconds = 3.00f;
        public float baselineStrongThreshold = 0.60f;
        public float baselineWeakThreshold = 0.45f;

        [Header("Adaptive Difficulty")]
        public AdaptiveDifficultyController adaptiveController;
        public bool adaptiveEnabled = false;
        public bool adaptiveSessionSeeded = false;
        public int adaptiveSeedDifficultyLevel = -1;

        [Header("Targets")]
        public PracticeGestureType targetGestureType = PracticeGestureType.OpenHand;
        public float targetHoldSeconds = 3.0f;

        [Header("Task Timing")]
        public float maxTaskSeconds = 10.0f;

        [Header("Rehabilitation-Friendly Matching Thresholds")]
        [Tooltip("Strong-match threshold: progress accumulates at the normal rate, and only a strong match can complete the task.")]
        [Range(0f, 1f)] public float strongMatchThreshold = 0.80f;

        [Tooltip("Weak-match threshold: provides encouraging progress and scoring feedback, but does not count directly towards task completion.")]
        [Range(0f, 1f)] public float weakMatchThreshold = 0.45f;

        [Tooltip("For a strong match, the target confidence must exceed the second-highest confidence by at least this margin.")]
        [Range(0f, 1f)] public float strongMatchMargin = 0.12f;

        [Tooltip("Visual progress multiplier for weak matches. It provides encouragement only and does not affect task completion.")]
        [Range(0f, 1f)] public float weakMatchProgressMultiplier = 0.35f;

        [Header("Relaxed Thresholds for the OK Gesture")]
        public bool relaxOKThresholds = true;
        [Range(0f, 1f)] public float okStrongMatchThreshold = 0.62f;
        [Range(0f, 1f)] public float okWeakMatchThreshold = 0.35f;
        [Range(0f, 1f)] public float okStrongMatchMargin = 0.03f;

        [Header("Auto Reset / Next QR")]
        public bool autoResetAfterSuccess = true;
        public bool autoResetAfterFailure = false;
        [Min(0f)] public float autoResetDelaySeconds = 1.5f;

        [Header("Status UI")]
        public TMP_Text txtStateValue;
        public TMP_Text txtTimerValue;
        public TMP_Text txtTrackingValue;

        [Header("Task UI (optional)")]
        public TMP_Text txtTaskNameValue;
        public TMP_Text txtInstructionValue;
        public TMP_Text txtTargetValue;

        [Header("Feedback UI (optional)")]
        public TMP_Text txtPrimaryMessage;
        public TMP_Text txtSecondaryMessage;
        public Image progressFill;

        [Header("Score UI (optional)")]
        public TMP_Text txtScoreValue;

        [Header("Buttons")]
        public Button btnStart;
        public Button btnPause;
        public Button btnReset;

        [Header("Log Export")]
        public bool exportLogs = true;
        public string sessionId = "practice_smoke_001";
        public string logFileName = "practice_log.csv";

        [Header("Unified Log Fields")]
        public int currentDifficultyLevel = 1;
        public string lastScannedCardId = "";
        public string currentTaskId = "";
        public string timestampTaskStartUtc = "";
        public string timestampTaskEndUtc = "";
        public int trackingLossCount = 0;

        [Header("Debug")]
        public bool debugLogs = false;

        private enum RunState { Idle, Running, Paused, Success, Failed }
        private RunState state = RunState.Idle;

        private float holdAccum = 0f;
        private float assistAccum = 0f;

        private float elapsedTime = 0f;
        private float matchAccum = 0f;
        private float lostTrackingAccum = 0f;

        private float targetConfidence = 0f;
        private float secondBestConfidence = 0f;
        private bool isStrongMatch = false;
        private bool isWeakMatch = false;

        private float trackedTimeAccum = 0f;
        private float targetConfidenceAccum = 0f;
        private float confidenceMarginAccum = 0f;
        private float strongMatchTimeAccum = 0f;
        private float weakMatchTimeAccum = 0f;

        private float debugNextTime = 0f;
        private bool hasExportedCurrentResult = false;
        private bool wasTrackingLastFrame = false;
        private Coroutine autoResetCoroutine = null;
        private bool autoResetPending = false;

        [Header("Unified Output")]
        public TrainingFeedbackState feedbackState = TrainingFeedbackState.Idle;
        public TrainingErrorType errorType = TrainingErrorType.None;

        [TextArea] public string primaryMessage = "Ready to start";
        [TextArea] public string secondaryMessage = "Show your hand to begin";

        [Range(0f, 1f)] public float progress01 = 0f;
        public bool isCompleted = false;
        public bool isSuccess = false;
        public float completionTime = 0f;

        [Header("Scoring Output")]
        [Range(0f, 100f)] public float resultScore = 0f;
        [Range(0f, 1f)] public float holdRatio = 0f;
        [Range(0f, 1f)] public float trackingRatio = 1f;
        [Range(0f, 1f)] public float assistRatio = 0f;

        public float holdScore = 0f;
        public float trackingScore = 0f;
        public float assistScore = 0f;
        public float penaltyScore = 0f;
        public float qualityScore = 0f;
        public float baseScore = 0f;
        public float effortBonus = 0f;

        [Header("Movement Quality Metrics")]
        [Range(0f, 1f)] public float avgTargetConfidence = 0f;
        [Range(0f, 1f)] public float avgConfidenceMargin = 0f;
        [Range(0f, 1f)] public float strongMatchRatio = 0f;
        [Range(0f, 1f)] public float weakMatchRatio = 0f;

        public float marginScore = 0f;
        public float consistencyScore = 0f;

        [Header("Confidence Debug")]
        [Range(0f, 1f)] public float debugOpenConfidence = 0f;
        [Range(0f, 1f)] public float debugFistConfidence = 0f;
        [Range(0f, 1f)] public float debugOKConfidence = 0f;
        [Range(0f, 1f)] public float debugThumbsUpConfidence = 0f;

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

            if (adaptiveController == null)
            {
                var allAdaptive = FindObjectsByType<AdaptiveDifficultyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (allAdaptive != null && allAdaptive.Length > 0) adaptiveController = allAdaptive[0];
            }
#else
            if (gestureInput == null) gestureInput = FindObjectOfType<ThesisGestureInput>();
            if (gestureRecognizer == null) gestureRecognizer = FindObjectOfType<HandGestureRecognizer>();
            if (adaptiveController == null) adaptiveController = FindObjectOfType<AdaptiveDifficultyController>();
#endif

            if (adaptiveController != null)
            {
                adaptiveController.targetPracticeController = this;
                adaptiveController.SyncFromPractice();
                adaptiveEnabled = adaptiveController.adaptiveEnabled;
            }

            if (debugLogs)
            {
                Debug.Log($"[Practice] Awake on {name} (scene={gameObject.scene.name})");
                Debug.Log(gestureInput != null ? $"[Practice] gestureInput bound = {gestureInput.name}" : "[Practice] gestureInput is NULL");
                Debug.Log(gestureRecognizer != null ? $"[Practice] gestureRecognizer bound = {gestureRecognizer.name}" : "[Practice] gestureRecognizer is NULL");
                Debug.Log(adaptiveController != null ? $"[Practice] adaptiveController bound = {adaptiveController.name}" : "[Practice] adaptiveController is NULL");
            }

            if (btnStart != null) btnStart.onClick.AddListener(OnStart);
            if (btnPause != null) btnPause.onClick.AddListener(OnPause);
            if (btnReset != null) btnReset.onClick.AddListener(OnReset);
            if (btnFixedMode != null) btnFixedMode.onClick.AddListener(OnClickFixedMode);
            if (btnAdaptiveMode != null) btnAdaptiveMode.onClick.AddListener(OnClickAdaptiveMode);

            if (btnGestureOpenHand != null) btnGestureOpenHand.onClick.AddListener(OnClickOpenHand);
            if (btnGestureFist != null) btnGestureFist.onClick.AddListener(OnClickFist);
            if (btnGestureOK != null) btnGestureOK.onClick.AddListener(OnClickOK);

            RefreshTaskUI();
            RefreshUnifiedOutput(false, "none");
            RecalculateScore();
            RefreshUI(false);
            RefreshModeAndDifficultyUI();
        }

        void OnDisable()
        {
            CancelAutoResetCountdown();
        }

        void Update()
        {
            bool tracking = gestureInput != null && gestureInput.IsTracking;
            string gesture = gestureInput != null ? gestureInput.currentGesture : "none";

            currentDifficultyLevel = ResolveDifficultyLevel();
            UpdateConfidenceCache(gesture);

            if (state == RunState.Running)
            {
                if (!tracking && wasTrackingLastFrame)
                    trackingLossCount++;
                elapsedTime += Time.deltaTime;

                if (!tracking)
                    lostTrackingAccum += Time.deltaTime;

                if (tracking)
                {
                    float dt = Time.deltaTime;

                    trackedTimeAccum += dt;
                    targetConfidenceAccum += targetConfidence * dt;
                    confidenceMarginAccum += Mathf.Clamp01(targetConfidence - secondBestConfidence) * dt;

                    if (isStrongMatch)
                    {
                        strongMatchTimeAccum += dt;
                        matchAccum += dt;
                        holdAccum += dt;
                    }
                    else if (isWeakMatch)
                    {
                        weakMatchTimeAccum += dt;
                        assistAccum += dt * weakMatchProgressMultiplier;
                        matchAccum += dt * weakMatchProgressMultiplier;
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
                        errorType = TrainingErrorType.None;
                        timestampTaskEndUtc = DateTime.UtcNow.ToString("o");
                        StartAutoResetCountdownIfNeeded(true);
                    }
                }

                if (elapsedTime >= maxTaskSeconds && state == RunState.Running)
                {
                    state = RunState.Failed;
                    isCompleted = true;
                    isSuccess = false;
                    completionTime = elapsedTime;
                    timestampTaskEndUtc = DateTime.UtcNow.ToString("o");

                    if (!tracking)
                        errorType = TrainingErrorType.TrackingLost;
                    else if (holdAccum > 0f || assistAccum > 0f)
                        errorType = TrainingErrorType.UnstableHold;
                    else
                        errorType = TrainingErrorType.GestureMismatch;

                    StartAutoResetCountdownIfNeeded(false);
                }
            }

            RefreshUnifiedOutput(tracking, gesture);
            RecalculateScore();
            RefreshUI(tracking);
            TryExportResultIfNeeded();
            RefreshModeAndDifficultyUI();

            if (btnStart != null) btnStart.interactable = (state == RunState.Idle || state == RunState.Paused);
            if (btnPause != null) btnPause.interactable = (state == RunState.Running);
            if (btnReset != null) btnReset.interactable = (state != RunState.Idle);

            wasTrackingLastFrame = tracking;

            if (debugLogs && Time.time >= debugNextTime)
            {
                debugNextTime = Time.time + 0.5f;
                Debug.Log(
                    $"[Practice] target={TargetGestureKey}, state={state}, tracking={tracking}, gesture={gesture}, " +
                    $"targetConf={targetConfidence:0.00}, secondConf={secondBestConfidence:0.00}, " +
                    $"open={debugOpenConfidence:0.00}, fist={debugFistConfidence:0.00}, ok={debugOKConfidence:0.00}, " +
                    $"strong={isStrongMatch}, weak={isWeakMatch}, " +
                    $"avgTargetConf={avgTargetConfidence:0.00}, avgMargin={avgConfidenceMargin:0.00}, " +
                    $"strongRatio={strongMatchRatio:0.00}, weakRatio={weakMatchRatio:0.00}, " +
                    $"hold={holdAccum:0.00}/{targetHoldSeconds:0.00}, assist={assistAccum:0.00}/{targetHoldSeconds:0.00}, " +
                    $"elapsed={elapsedTime:0.00}/{maxTaskSeconds:0.00}, feedback={feedbackState}, error={errorType}, " +
                    $"score={resultScore:0.0}, autoResetPending={autoResetPending}, adaptiveEnabled={adaptiveEnabled}, " +
                    $"adaptiveSeeded={adaptiveSessionSeeded}, adaptiveSeedLevel={adaptiveSeedDifficultyLevel}, level={currentDifficultyLevel}"
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
                debugThumbsUpConfidence = 0f;
                return;
            }

            debugOpenConfidence = gestureRecognizer.OpenConfidence;
            debugFistConfidence = gestureRecognizer.FistConfidence;
            debugOKConfidence = gestureRecognizer.OKConfidence;
            debugThumbsUpConfidence = gestureRecognizer.ThumbsUpConfidence;

            float openC = gestureRecognizer.OpenConfidence;
            float fistC = gestureRecognizer.FistConfidence;
            float okC = gestureRecognizer.OKConfidence;
            float thumbsUpC = gestureRecognizer.ThumbsUpConfidence;

            switch (targetGestureType)
            {
                case PracticeGestureType.Fist:
                    targetConfidence = fistC;
                    secondBestConfidence = Mathf.Max(openC, Mathf.Max(okC, thumbsUpC));
                    break;
                case PracticeGestureType.OK:
                    targetConfidence = okC;
                    secondBestConfidence = Mathf.Max(openC, Mathf.Max(fistC, thumbsUpC));
                    break;
                case PracticeGestureType.ThumbsUp:
                    targetConfidence = thumbsUpC;
                    secondBestConfidence = Mathf.Max(openC, Mathf.Max(fistC, okC));
                    break;
                case PracticeGestureType.OpenHand:
                default:
                    targetConfidence = openC;
                    secondBestConfidence = Mathf.Max(fistC, Mathf.Max(okC, thumbsUpC));
                    break;
            }

            bool labelMatches = currentGesture == TargetGestureKey;

            if (relaxOKThresholds && targetGestureType == PracticeGestureType.OK)
            {
                isStrongMatch = labelMatches && targetConfidence >= 0.20f;
                isWeakMatch = !isStrongMatch && labelMatches;
                return;
            }

            bool strongEnough = targetConfidence >= strongMatchThreshold;
            bool marginEnough = (targetConfidence - secondBestConfidence) >= strongMatchMargin;

            isStrongMatch = labelMatches && strongEnough && marginEnough;
            isWeakMatch = !isStrongMatch && labelMatches && targetConfidence >= weakMatchThreshold;
        }

        private void RefreshTaskUI()
        {
            if (txtTaskNameValue != null)
                txtTaskNameValue.text = TargetGestureDisplay;

            if (txtInstructionValue != null)
                txtInstructionValue.text = $"Keep your hand in frame and try to hold {TargetGestureDisplay} for {targetHoldSeconds:0.0} seconds";

            if (txtTargetValue != null)
                txtTargetValue.text = $"Target: {TargetGestureDisplay} | Hold: {targetHoldSeconds:0.0}s";
        }

        private void RefreshUnifiedOutput(bool tracking, string gesture)
        {
            float visualAccum = Mathf.Max(holdAccum, assistAccum);
            progress01 = targetHoldSeconds > 0f ? Mathf.Clamp01(visualAccum / targetHoldSeconds) : 0f;

            switch (state)
            {
                case RunState.Idle:
                    feedbackState = TrainingFeedbackState.Idle;
                    errorType = TrainingErrorType.None;
                    primaryMessage = "Ready to start";
                    secondaryMessage = "Scan the next QR card or press Start";
                    isCompleted = false;
                    isSuccess = false;
                    break;

                case RunState.Paused:
                    feedbackState = TrainingFeedbackState.Idle;
                    primaryMessage = "Paused";
                    secondaryMessage = "Press Start to continue";
                    break;

                case RunState.Success:
                    feedbackState = TrainingFeedbackState.Success;
                    primaryMessage = "Success";
                    secondaryMessage = autoResetAfterSuccess
                        ? $"Task completed. Ready for next QR in {autoResetDelaySeconds:0.0}s"
                        : $"You completed {TargetGestureDisplay}";
                    break;

                case RunState.Failed:
                    feedbackState = TrainingFeedbackState.Failed;
                    switch (errorType)
                    {
                        case TrainingErrorType.TrackingLost:
                            primaryMessage = "Tracking lost";
                            secondaryMessage = autoResetAfterFailure
                                ? $"Returning to scan-ready state in {autoResetDelaySeconds:0.0}s"
                                : "Please move your hand into view";
                            break;
                        case TrainingErrorType.GestureMismatch:
                            primaryMessage = "Try again";
                            secondaryMessage = autoResetAfterFailure
                                ? $"Returning to scan-ready state in {autoResetDelaySeconds:0.0}s"
                                : $"Keep moving toward {TargetGestureDisplay}";
                            break;
                        case TrainingErrorType.UnstableHold:
                            primaryMessage = "Almost there";
                            secondaryMessage = autoResetAfterFailure
                                ? $"Returning to scan-ready state in {autoResetDelaySeconds:0.0}s"
                                : "Good attempt. Try to hold more steadily";
                            break;
                        case TrainingErrorType.Timeout:
                        default:
                            primaryMessage = "Time out";
                            secondaryMessage = autoResetAfterFailure
                                ? $"Returning to scan-ready state in {autoResetDelaySeconds:0.0}s"
                                : "Please try again";
                            break;
                    }
                    break;

                case RunState.Running:
                    if (!tracking)
                    {
                        feedbackState = TrainingFeedbackState.TrackingLost;
                        primaryMessage = "Hand not detected";
                        secondaryMessage = "Please move your hand into view";
                    }
                    else if (isStrongMatch)
                    {
                        feedbackState = TrainingFeedbackState.Holding;
                        primaryMessage = "Hold steady";
                        secondaryMessage = $"Good match. Keep {TargetGestureDisplay} for {targetHoldSeconds:0.0}s";
                    }
                    else if (isWeakMatch)
                    {
                        feedbackState = TrainingFeedbackState.Holding;
                        primaryMessage = "Almost there";
                        secondaryMessage = $"Good attempt. Keep moving toward {TargetGestureDisplay}";
                    }
                    else
                    {
                        feedbackState = TrainingFeedbackState.Ready;
                        primaryMessage = "Make the target gesture";
                        secondaryMessage = $"Target: {TargetGestureDisplay}";
                    }
                    break;
            }
        }

        private void RecalculateScore()
        {
            holdRatio = targetHoldSeconds > 0f ? Mathf.Clamp01(holdAccum / targetHoldSeconds) : 0f;
            assistRatio = targetHoldSeconds > 0f ? Mathf.Clamp01(assistAccum / targetHoldSeconds) : 0f;
            trackingRatio = maxTaskSeconds > 0f ? Mathf.Clamp01(1f - (lostTrackingAccum / maxTaskSeconds)) : 1f;

            if (trackedTimeAccum > 0f)
            {
                avgTargetConfidence = Mathf.Clamp01(targetConfidenceAccum / trackedTimeAccum);
                avgConfidenceMargin = Mathf.Clamp01(confidenceMarginAccum / trackedTimeAccum);
                strongMatchRatio = Mathf.Clamp01(strongMatchTimeAccum / trackedTimeAccum);
                weakMatchRatio = Mathf.Clamp01(weakMatchTimeAccum / trackedTimeAccum);
            }
            else
            {
                avgTargetConfidence = 0f;
                avgConfidenceMargin = 0f;
                strongMatchRatio = 0f;
                weakMatchRatio = 0f;
            }

            penaltyScore = GetPenalty(errorType);

            if (isSuccess)
            {
                baseScore = 20f;
                holdScore = holdRatio * 25f;
                trackingScore = trackingRatio * 15f;
                qualityScore = avgTargetConfidence * 15f;
                marginScore = avgConfidenceMargin * 10f;
                consistencyScore = strongMatchRatio * 15f;
                assistScore = weakMatchRatio * 10f;
                effortBonus = 0f;

                float rawScore =
                    baseScore + holdScore + trackingScore + qualityScore + marginScore + consistencyScore -
                    assistScore - penaltyScore;

                resultScore = Mathf.Clamp(rawScore, 0f, 100f);
            }
            else
            {
                baseScore = 10f;
                holdScore = holdRatio * 20f;
                trackingScore = trackingRatio * 10f;
                qualityScore = avgTargetConfidence * 10f;
                marginScore = avgConfidenceMargin * 5f;
                consistencyScore = strongMatchRatio * 10f;
                assistScore = assistRatio * 10f;
                effortBonus = weakMatchRatio * 5f;

                float rawScore =
                    baseScore + holdScore + trackingScore + qualityScore + marginScore + consistencyScore +
                    assistScore + effortBonus - penaltyScore;

                resultScore = Mathf.Clamp(rawScore, 0f, 100f);
            }
        }

        private float GetPenalty(TrainingErrorType type)
        {
            switch (type)
            {
                case TrainingErrorType.TrackingLost: return 5f;
                case TrainingErrorType.GestureMismatch: return 6f;
                case TrainingErrorType.UnstableHold: return 4f;
                case TrainingErrorType.Timeout: return 3f;
                case TrainingErrorType.None:
                default: return 0f;
            }
        }

        private void RefreshUI(bool tracking)
        {
            if (txtTrackingValue != null)
                txtTrackingValue.text = tracking ? "Present" : "Lost";

            if (txtStateValue != null)
                txtStateValue.text = state.ToString();

            if (txtTimerValue != null)
                txtTimerValue.text = $"{holdAccum:0.0}/{targetHoldSeconds:0.0}s";

            if (txtPrimaryMessage != null)
                txtPrimaryMessage.text = primaryMessage;

            if (txtSecondaryMessage != null)
                txtSecondaryMessage.text = secondaryMessage;

            if (progressFill != null)
                progressFill.fillAmount = progress01;

            if (txtScoreValue != null)
                txtScoreValue.text = $"{resultScore:0}";
        }

        private void TryExportResultIfNeeded()
        {
            if (!isCompleted) return;
            if (hasExportedCurrentResult) return;

            if (exportLogs)
                ExportPracticeLog();

            if (adaptiveController != null)
            {
                AdaptiveTaskResult r = new AdaptiveTaskResult
                {
                    gestureType = TargetGestureDisplay,
                    success = isSuccess,
                    score = resultScore,
                    errorType = errorType,
                    trackingRatio = trackingRatio,
                    strongMatchRatio = strongMatchRatio,
                    completionTimeSec = completionTime,
                    timestampUtc = DateTime.UtcNow.ToString("o")
                };

                adaptiveController.RegisterTaskResult(r);
            }

            hasExportedCurrentResult = true;
        }

        private void ExportPracticeLog()
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "PracticeLogs");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, logFileName);
                bool fileExists = File.Exists(filePath);

                using (var sw = new StreamWriter(filePath, true))
                {
                    if (!fileExists)
                    {
                        sw.WriteLine(
                            "session_id,timestamp_utc,module,difficulty_mode,difficulty_level,scanned_card_id,task_id,target_gesture,success,score,error_type,feedback_state," +
                            "completion_time_sec,target_hold_sec,hold_ratio,assist_ratio,tracking_ratio,tracking_loss_count,tracking_loss_duration_sec," +
                            "timestamp_task_start_utc,timestamp_task_end_utc,target_confidence," +
                            "avg_target_confidence,avg_confidence_margin,strong_match_ratio,weak_match_ratio," +
                            "margin_score,consistency_score," +
                            "open_confidence,fist_confidence,ok_confidence,thumbs_up_confidence," +
                            "strong_match_threshold,weak_match_threshold,strong_match_margin," +
                            "relax_ok_thresholds,ok_strong_match_threshold,ok_weak_match_threshold,ok_strong_match_margin"
                        );
                    }

                    string line =
                        EscapeCsv(sessionId) + "," +
                        EscapeCsv(DateTime.UtcNow.ToString("o")) + "," +
                        EscapeCsv("Practice") + "," +
                        EscapeCsv(GetDifficultyMode()) + "," +
                        currentDifficultyLevel.ToString() + "," +
                        EscapeCsv(lastScannedCardId) + "," +
                        EscapeCsv(currentTaskId) + "," +
                        EscapeCsv(TargetGestureDisplay) + "," +
                        isSuccess.ToString().ToLowerInvariant() + "," +
                        resultScore.ToString("0.0") + "," +
                        EscapeCsv(errorType.ToString()) + "," +
                        EscapeCsv(feedbackState.ToString()) + "," +
                        completionTime.ToString("0.00") + "," +
                        targetHoldSeconds.ToString("0.00") + "," +
                        holdRatio.ToString("0.000") + "," +
                        assistRatio.ToString("0.000") + "," +
                        trackingRatio.ToString("0.000") + "," +
                        trackingLossCount.ToString() + "," +
                        lostTrackingAccum.ToString("0.000") + "," +
                        EscapeCsv(timestampTaskStartUtc) + "," +
                        EscapeCsv(timestampTaskEndUtc) + "," +
                        targetConfidence.ToString("0.000") + "," +
                        avgTargetConfidence.ToString("0.000") + "," +
                        avgConfidenceMargin.ToString("0.000") + "," +
                        strongMatchRatio.ToString("0.000") + "," +
                        weakMatchRatio.ToString("0.000") + "," +
                        marginScore.ToString("0.000") + "," +
                        consistencyScore.ToString("0.000") + "," +
                        debugOpenConfidence.ToString("0.000") + "," +
                        debugFistConfidence.ToString("0.000") + "," +
                        debugOKConfidence.ToString("0.000") + "," +
                        debugThumbsUpConfidence.ToString("0.000") + "," +
                        strongMatchThreshold.ToString("0.000") + "," +
                        weakMatchThreshold.ToString("0.000") + "," +
                        strongMatchMargin.ToString("0.000") + "," +
                        relaxOKThresholds.ToString().ToLowerInvariant() + "," +
                        okStrongMatchThreshold.ToString("0.000") + "," +
                        okWeakMatchThreshold.ToString("0.000") + "," +
                        okStrongMatchMargin.ToString("0.000");

                    sw.WriteLine(line);
                }

                if (debugLogs)
                    Debug.Log($"[Practice] Log exported -> {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Practice] ExportPracticeLog failed: {ex.Message}");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            string escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private void OnStart()
        {
            if (debugLogs) Debug.Log("[Practice] OnStart()");
            CancelAutoResetCountdown();

            if (state == RunState.Success) return;

            if (state == RunState.Failed)
                ResetRuntimeStateOnly();

            if (state != RunState.Paused && string.IsNullOrEmpty(timestampTaskStartUtc))
            {
                currentTaskId = Guid.NewGuid().ToString("N");
                timestampTaskStartUtc = DateTime.UtcNow.ToString("o");
                timestampTaskEndUtc = "";
                trackingLossCount = 0;
                wasTrackingLastFrame = gestureInput != null && gestureInput.IsTracking;
            }

            RefreshTaskUI();
            state = RunState.Running;
        }

        private void OnPause()
        {
            if (debugLogs) Debug.Log("[Practice] OnPause()");
            CancelAutoResetCountdown();
            if (state == RunState.Running) state = RunState.Paused;
        }

        private void OnReset()
        {
            if (debugLogs) Debug.Log("[Practice] OnReset()");
            PerformResetToIdle();
        }

        private void PerformResetToIdle()
        {
            CancelAutoResetCountdown();
            ResetRuntimeStateOnly();
            state = RunState.Idle;
            RefreshTaskUI();
            RefreshUnifiedOutput(
                gestureInput != null && gestureInput.IsTracking,
                gestureInput != null ? gestureInput.currentGesture : "none"
            );
            RecalculateScore();
            RefreshUI(gestureInput != null && gestureInput.IsTracking);
            RefreshModeAndDifficultyUI();
        }

        private void StartAutoResetCountdownIfNeeded(bool success)
        {
            bool enabled = success ? autoResetAfterSuccess : autoResetAfterFailure;
            if (!enabled || autoResetPending) return;

            CancelAutoResetCountdown();
            autoResetCoroutine = StartCoroutine(AutoResetAfterDelay());
            autoResetPending = true;

            if (debugLogs)
                Debug.Log($"[Practice] Auto reset scheduled after {(success ? "success" : "failure")} in {autoResetDelaySeconds:0.0}s");
        }

        private IEnumerator AutoResetAfterDelay()
        {
            float delay = Mathf.Max(0f, autoResetDelaySeconds);
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            autoResetCoroutine = null;
            autoResetPending = false;
            PerformResetToIdle();
        }

        private void CancelAutoResetCountdown()
        {
            if (autoResetCoroutine != null)
            {
                StopCoroutine(autoResetCoroutine);
                autoResetCoroutine = null;
            }

            autoResetPending = false;
        }

        private void OnClickFixedMode()
        {
            adaptiveEnabled = false;
            adaptiveSessionSeeded = false;
            adaptiveSeedDifficultyLevel = -1;

            ApplyBaselineDifficulty();

            if (adaptiveController != null)
            {
                adaptiveController.adaptiveEnabled = false;
                adaptiveController.ResetAdaptiveSession(
                    baselineHoldSeconds,
                    baselineStrongThreshold,
                    baselineWeakThreshold
                );
            }

            ResetForModeOrGestureSwitch();
            RefreshModeAndDifficultyUI();
        }

        private void OnClickAdaptiveMode()
        {
            adaptiveEnabled = true;
            adaptiveSessionSeeded = false;
            adaptiveSeedDifficultyLevel = -1;

            ApplyBaselineDifficulty();

            if (adaptiveController != null)
            {
                adaptiveController.adaptiveEnabled = true;
                adaptiveController.ResetAdaptiveSession(
                    baselineHoldSeconds,
                    baselineStrongThreshold,
                    baselineWeakThreshold
                );
            }

            ResetForModeOrGestureSwitch();
            RefreshModeAndDifficultyUI();
        }

        public void SetAdaptiveMode(bool enabled)
        {
            if (enabled == adaptiveEnabled && adaptiveSessionSeeded)
            {
                if (adaptiveController != null)
                    adaptiveController.adaptiveEnabled = enabled;
                RefreshModeAndDifficultyUI();
                return;
            }

            if (enabled)
                OnClickAdaptiveMode();
            else
                OnClickFixedMode();
        }

        public void SetScannedCardContext(string cardId, int difficultyLevel)
        {
            lastScannedCardId = cardId ?? "";
            currentDifficultyLevel = Mathf.Clamp(difficultyLevel, 1, 4);
            RefreshModeAndDifficultyUI();
        }

        public void ApplyScannedTask(
            string cardId,
            int difficultyLevel,
            string gestureEnumName,
            bool useAdaptive,
            float holdSecs,
            float maxTaskSecs,
            float strongThr,
            float weakThr,
            float strongMarginVal,
            float weakProgMult)
        {
            PracticeGestureType parsedGesture;
            if (!TryParseGestureEnum(gestureEnumName, out parsedGesture))
                parsedGesture = targetGestureType;

            targetGestureType = parsedGesture;
            lastScannedCardId = cardId ?? "";
            currentDifficultyLevel = Mathf.Clamp(difficultyLevel, 1, 4);

            if (useAdaptive)
            {
                adaptiveEnabled = true;
                if (adaptiveController != null)
                    adaptiveController.adaptiveEnabled = true;

                bool shouldReseed = !adaptiveSessionSeeded || adaptiveSeedDifficultyLevel != currentDifficultyLevel;
                if (shouldReseed)
                {
                    targetHoldSeconds = holdSecs;
                    maxTaskSeconds = maxTaskSecs;
                    strongMatchThreshold = strongThr;
                    weakMatchThreshold = weakThr;
                    strongMatchMargin = strongMarginVal;
                    weakMatchProgressMultiplier = weakProgMult;

                    if (adaptiveController != null)
                        adaptiveController.ResetAdaptiveSession(holdSecs, strongThr, weakThr);

                    adaptiveSessionSeeded = true;
                    adaptiveSeedDifficultyLevel = currentDifficultyLevel;
                }
            }
            else
            {
                adaptiveEnabled = false;
                adaptiveSessionSeeded = false;
                adaptiveSeedDifficultyLevel = -1;

                targetHoldSeconds = holdSecs;
                maxTaskSeconds = maxTaskSecs;
                strongMatchThreshold = strongThr;
                weakMatchThreshold = weakThr;
                strongMatchMargin = strongMarginVal;
                weakMatchProgressMultiplier = weakProgMult;

                if (adaptiveController != null)
                {
                    adaptiveController.adaptiveEnabled = false;
                    adaptiveController.ResetAdaptiveSession(holdSecs, strongThr, weakThr);
                }
            }

            ResetForNextScannedTaskOnly();
            RefreshModeAndDifficultyUI();

            if (debugLogs)
            {
                Debug.Log(
                    $"[Practice] ApplyScannedTask -> card={lastScannedCardId}, gesture={targetGestureType}, " +
                    $"level={currentDifficultyLevel}, adaptive={adaptiveEnabled}, seeded={adaptiveSessionSeeded}, " +
                    $"seedLevel={adaptiveSeedDifficultyLevel}, hold={targetHoldSeconds:0.00}, " +
                    $"strong={strongMatchThreshold:0.00}, weak={weakMatchThreshold:0.00}"
                );
            }
        }

        private void OnClickOpenHand()
        {
            SetGestureAndReset(PracticeGestureType.OpenHand);
        }

        private void OnClickFist()
        {
            SetGestureAndReset(PracticeGestureType.Fist);
        }

        private void OnClickOK()
        {
            SetGestureAndReset(PracticeGestureType.OK);
        }

        private void SetGestureAndReset(PracticeGestureType newGesture)
        {
            targetGestureType = newGesture;

            ApplyBaselineDifficulty();

            adaptiveSessionSeeded = false;
            adaptiveSeedDifficultyLevel = -1;

            if (adaptiveController != null)
            {
                adaptiveController.ResetAdaptiveSession(
                    baselineHoldSeconds,
                    baselineStrongThreshold,
                    baselineWeakThreshold
                );
            }

            ResetForModeOrGestureSwitch();
            RefreshModeAndDifficultyUI();
        }

        private void ApplyBaselineDifficulty()
        {
            targetHoldSeconds = baselineHoldSeconds;
            strongMatchThreshold = baselineStrongThreshold;
            weakMatchThreshold = baselineWeakThreshold;
        }

        private void ResetForModeOrGestureSwitch()
        {
            CancelAutoResetCountdown();
            ResetRuntimeStateOnly();
            state = RunState.Idle;

            RefreshTaskUI();

            bool tracking = gestureInput != null && gestureInput.IsTracking;
            string gesture = gestureInput != null ? gestureInput.currentGesture : "none";

            RefreshUnifiedOutput(tracking, gesture);
            RecalculateScore();
            RefreshUI(tracking);
        }

        private void ResetForNextScannedTaskOnly()
        {
            CancelAutoResetCountdown();
            ResetRuntimeStateOnly();
            state = RunState.Idle;

            RefreshTaskUI();

            bool tracking = gestureInput != null && gestureInput.IsTracking;
            string gesture = gestureInput != null ? gestureInput.currentGesture : "none";

            RefreshUnifiedOutput(tracking, gesture);
            RecalculateScore();
            RefreshUI(tracking);
        }

        private void RefreshModeAndDifficultyUI()
        {
            if (adaptiveController != null)
                adaptiveEnabled = adaptiveController.adaptiveEnabled;

            if (txtModeValue != null)
                txtModeValue.text = adaptiveEnabled ? "Adaptive" : "Fixed";

            if (txtDifficultyValue != null)
                txtDifficultyValue.text = $"L{currentDifficultyLevel} H={targetHoldSeconds:0.00} S={strongMatchThreshold:0.00} W={weakMatchThreshold:0.00}";
        }

        private void ResetRuntimeStateOnly()
        {
            holdAccum = 0f;
            assistAccum = 0f;
            elapsedTime = 0f;
            matchAccum = 0f;
            lostTrackingAccum = 0f;

            targetConfidence = 0f;
            secondBestConfidence = 0f;
            isStrongMatch = false;
            isWeakMatch = false;

            trackedTimeAccum = 0f;
            targetConfidenceAccum = 0f;
            confidenceMarginAccum = 0f;
            strongMatchTimeAccum = 0f;
            weakMatchTimeAccum = 0f;

            progress01 = 0f;
            isCompleted = false;
            isSuccess = false;
            completionTime = 0f;
            errorType = TrainingErrorType.None;

            resultScore = 0f;
            holdRatio = 0f;
            assistRatio = 0f;
            trackingRatio = 1f;

            holdScore = 0f;
            trackingScore = 0f;
            assistScore = 0f;
            penaltyScore = 0f;
            qualityScore = 0f;
            baseScore = 0f;
            effortBonus = 0f;

            avgTargetConfidence = 0f;
            avgConfidenceMargin = 0f;
            strongMatchRatio = 0f;
            weakMatchRatio = 0f;
            marginScore = 0f;
            consistencyScore = 0f;

            currentTaskId = "";
            timestampTaskStartUtc = "";
            timestampTaskEndUtc = "";
            trackingLossCount = 0;
            wasTrackingLastFrame = false;
            hasExportedCurrentResult = false;
        }

        private bool TryParseGestureEnum(string enumName, out PracticeGestureType parsed)
        {
            parsed = PracticeGestureType.OpenHand;
            if (string.IsNullOrWhiteSpace(enumName))
                return false;

            try
            {
                parsed = (PracticeGestureType)Enum.Parse(typeof(PracticeGestureType), enumName, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetGestureKey(PracticeGestureType type)
        {
            switch (type)
            {
                case PracticeGestureType.Fist:
                    return "fist";
                case PracticeGestureType.OK:
                    return "ok";
                case PracticeGestureType.ThumbsUp:
                    return "thumbs_up";
                case PracticeGestureType.OpenHand:
                default:
                    return "open_hand";
            }
        }

        private int ResolveDifficultyLevel()
        {
            if (adaptiveEnabled && adaptiveController != null)
            {
                float hold = adaptiveController.currentHoldSeconds;
                int levelFromHold = 1 + Mathf.RoundToInt((hold - 2.0f) / 0.5f);
                return Mathf.Clamp(levelFromHold, 1, 4);
            }

            return Mathf.Clamp(currentDifficultyLevel, 1, 4);
        }

        private string GetDifficultyMode()
        {
            if (adaptiveController != null)
                adaptiveEnabled = adaptiveController.adaptiveEnabled;

            return adaptiveEnabled ? "adaptive" : "fixed";
        }

        private string GetGestureDisplayName(PracticeGestureType type)
        {
            switch (type)
            {
                case PracticeGestureType.Fist:
                    return "Fist";
                case PracticeGestureType.OK:
                    return "OK";
                case PracticeGestureType.ThumbsUp:
                    return "Thumbs Up";
                case PracticeGestureType.OpenHand:
                default:
                    return "Open Hand";
            }
        }
    }
}
