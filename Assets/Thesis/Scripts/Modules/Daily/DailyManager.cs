using UnityEngine;
using Thesis.Core;
using Thesis.Modules.Practice; // Reused for PracticeTaskConfigFile, PracticeConfigLoader, and QualityScorerV1

namespace Thesis.Modules.Daily
{
    public class DailyManager : MonoBehaviour, IHubModeRunner
    {
        [Header("References")]
        public ThesisSessionLogger logger;

        [Header("Config")]
        [Tooltip("If true, loads tasks from Resources/Configs/daily_tasks_v1.json")]
        public bool useConfig = true;

        private PracticeTaskConfigFile _cfg;
        private int _taskIndex = 0;

        // ===== IHubModeRunner =====
        public string ModeId => "Daily";
        public bool HasActiveSession => logger != null && logger.HasActiveSession;

        private void Awake()
        {
            // 1) Locate the shared logger automatically.
            // It is created under ThesisHub -> ThesisServices and kept alive with DontDestroyOnLoad.
            if (logger == null)
                logger = FindFirstObjectByType<ThesisSessionLogger>();

            if (logger == null)
            {
                Debug.LogWarning(
                    "[Daily] ThesisSessionLogger not found. " +
                    "Please start Play from ThesisHub and enter Daily via the Hub button (logger is DontDestroyOnLoad)."
                );
            }

            // 2) Load the optional task configuration.
            if (useConfig)
            {
                _cfg = PracticeConfigLoader.LoadFromResources("Configs/daily_tasks_v1");
                if (_cfg == null)
                {
                    Debug.LogWarning("[Daily] Config load failed. Will use default stub task parameters.");
                }
            }

            _taskIndex = 0;
        }

        // ===== Hub callbacks (used by the four main buttons in ThesisHub) =====
        public void HubStart() => StartDailySession();

        // HubAddFake adds one random task result, either a success or a failure.
        public void HubAddFake() => AddFakeTask();

        // HubEnd and HubExport are separate so the Hub can keep individual End and Export buttons.
        public void HubEnd() => EndSessionOnly();
        public void HubExport() => ExportOnly();

        // ===== UI callbacks (stable entry points for buttons in the Daily scene) =====
        public void UIStart() => StartDailySession();
        public void UISuccessTask() => CompleteTaskSuccess();
        public void UIFailTask() => CompleteTaskFail();

        // Use this for a button labelled "End + Export".
        public void UIEndAndExport() => EndAndExport();

        // ===== Original public methods (kept for existing OnClick bindings) =====
        public void StartDailySession()
        {
            if (logger == null)
            {
                Debug.LogError("[Daily] Cannot start session: logger is null.");
                return;
            }

            if (logger.HasActiveSession)
            {
                Debug.LogWarning("[Daily] Session already active. Use End first.");
                return;
            }

            logger.mode = "Daily";
            logger.fixedOrAdaptive = "fixed";
            logger.StartSession();

            _taskIndex = 0;
        }

        public void CompleteTaskSuccess() => AddOneTask(success: true);
        public void CompleteTaskFail() => AddOneTask(success: false);

        /// Adds a single random task result for the Hub button or other one-click testing.
        public void AddFakeTask()
        {
            if (logger == null)
            {
                Debug.LogError("[Daily] Cannot add task: logger is null.");
                return;
            }

            if (!logger.HasActiveSession)
            {
                Debug.LogWarning("[Daily] No active session. Press Start first.");
                return;
            }

            // Daily mode uses a slightly higher default success rate.
            bool success = Random.value > 0.30f;
            AddOneTask(success);
        }

        /// One-click "End + Export": closes the current session and exports it.
        public void EndAndExport()
        {
            if (logger == null)
            {
                Debug.LogError("[Daily] Cannot end/export: logger is null.");
                return;
            }

            if (!logger.HasActiveSession)
            {
                Debug.LogWarning("[Daily] No active session. Press Start first.");
                return;
            }

            logger.EndSession();
            logger.ExportSession();
        }

        /// Ends the session only. Used by the Hub and available if the controls are split later.
        public void EndSessionOnly()
        {
            if (logger == null)
            {
                Debug.LogError("[Daily] logger is null.");
                return;
            }

            if (!logger.HasActiveSession)
            {
                Debug.LogWarning("[Daily] No active session.");
                return;
            }

            logger.EndSession();
        }

        /// Exports the session only. As in Practice mode, the session must already be ended.
        public void ExportOnly()
        {
            if (logger == null)
            {
                Debug.LogError("[Daily] logger is null.");
                return;
            }

            if (logger.HasActiveSession)
            {
                Debug.LogWarning("[Daily] End session before export.");
                return;
            }

            logger.ExportSession();
        }

        private void AddOneTask(bool success)
        {
            if (logger == null)
            {
                Debug.LogError("[Daily] Cannot add task: logger is null.");
                return;
            }

            if (!logger.HasActiveSession)
            {
                Debug.LogWarning("[Daily] No active session. Press Start first.");
                return;
            }

            // 1) Select the current task configuration, falling back to defaults.
            string gestureType = "pinch";
            float holdTime = 1.5f;
            float tolerance = 0.12f;
            float rhythmSpeed = 1.0f;
            int seqLen = 1;
            int hint = 2;

            if (useConfig && _cfg != null && _cfg.tasks != null && _cfg.tasks.Count > 0)
            {
                var t = _cfg.tasks[_taskIndex % _cfg.tasks.Count];
                gestureType = t.gesture_type;
                holdTime = t.hold_time;
                tolerance = t.tolerance;
                rhythmSpeed = t.rhythm_speed;
                seqLen = t.sequence_len;
                hint = t.hint_intensity;

                _taskIndex++;
            }

            // 2) Generate placeholder feature values. These can later be replaced by TrackingProvider or TaskEngine outputs.
            float completionTime = 2.8f + Random.value * 1.5f;

            float featStability = success ? (0.55f + Random.value * 0.35f) : (0.20f + Random.value * 0.45f);
            float featHoldRatio = success ? (0.90f + Random.value * 0.15f) : (0.40f + Random.value * 0.55f);
            float featResponse = success ? (0.35f + Random.value * 0.70f) : (0.60f + Random.value * 1.20f);
            float featRange = success ? (0.55f + Random.value * 0.40f) : (0.25f + Random.value * 0.50f);
            float meanConf = success ? (0.65f + Random.value * 0.30f) : (0.45f + Random.value * 0.35f);

            int lossCount = Random.Range(0, success ? 2 : 3);
            float lossDur = Random.value * (success ? 0.25f : 0.60f);

            // 3) Calculate the score and error type.
            var (score, errorType) = QualityScorerV1.Score(
                featStability,
                featHoldRatio,
                featResponse,
                featRange,
                meanConf,
                lossCount,
                lossDur,
                completionTime
            );

            if (!success && errorType == "none")
                errorType = "failed_other";

            // 4) Write the task record.
            logger.AddTaskRecord(
                gestureType,
                success,
                completionTime,
                score,
                errorType,
                featStability,
                featHoldRatio,
                featResponse,
                featRange,
                meanConf,
                lossCount,
                lossDur,
                holdTime,
                tolerance,
                rhythmSpeed,
                seqLen,
                hint
            );
        }
    }
}
