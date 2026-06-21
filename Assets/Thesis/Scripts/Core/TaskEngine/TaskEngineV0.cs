using UnityEngine;
using Thesis.Modules.Practice;

namespace Thesis.Core
{
    /// Unified task execution engine (stub version).
    /// replace GenerateStubFeatures with real TrackingProvider/TaskEngine logic.
    public class TaskEngineV0 : MonoBehaviour
    {
        [Header("Debug")]
        [Tooltip("If true, prints task start/finish logs.")]
        public bool verboseLogs = false;

        private bool _running;
        private float _taskStartRealtime;

        public bool HasActiveTask => _running;

        /// Start a task (records timestamps, resets state).
        public void StartTask()
        {
            if (_running)
            {
                if (verboseLogs) Debug.LogWarning("[TaskEngineV0] StartTask called while running. Ignored.");
                return;
            }

            _running = true;
            _taskStartRealtime = Time.realtimeSinceStartup;

            if (verboseLogs) Debug.Log("[TaskEngineV0] Task started.");
        }

        /// Complete current task and generate a TaskResult (stub features for now).
        public TaskResult CompleteTask(PracticeTaskConfig cfg, bool success)
        {
            if (!_running)
            {
                if (verboseLogs) Debug.LogWarning("[TaskEngineV0] CompleteTask called while not running. Auto-starting.");
                StartTask();
            }

            var result = new TaskResult();
            result.success = success;

            // timestamps
            result.timestampTaskStartUtc = ExportUtils.IsoNowUtc(); // start stamp (UTC ISO)
            // NOTE: For strict start/end, you'd store start timestamp at StartTask. Week5 keep it simple.

            // completion time (proxy)
            float dt = Time.realtimeSinceStartup - _taskStartRealtime;
            // If dt is too small (button click immediately), fall back to a sensible stub range
            result.completionTimeSec = (dt < 0.05f) ? (2.0f + Random.value * 1.8f) : dt;

            // stub feature generation
            GenerateStubFeatures(cfg, success, result);

            result.timestampTaskEndUtc = ExportUtils.IsoNowUtc();

            _running = false;

            if (verboseLogs)
            {
                Debug.Log($"[TaskEngineV0] Task completed. success={success}, t={result.completionTimeSec:F2}s, loss={result.trackingLossCount}/{result.trackingLossDurationSec:F2}s");
            }

            return result;
        }

        /// Convenience: one-shot run for current button-driven workflow.
        /// Press Success/Fail -> we generate one TaskResult immediately.
        public TaskResult RunOneTask(PracticeTaskConfig cfg, bool success)
        {
            StartTask();
            return CompleteTask(cfg, success);
        }

        private void GenerateStubFeatures(PracticeTaskConfig cfg, bool success, TaskResult r)
        {
            // baseline ranges (success vs fail)
            r.featStability   = success ? (0.55f + Random.value * 0.35f) : (0.20f + Random.value * 0.45f);
            r.featHoldRatio   = success ? (0.90f + Random.value * 0.15f) : (0.35f + Random.value * 0.55f);
            r.featResponseTime= success ? (0.35f + Random.value * 0.70f) : (0.60f + Random.value * 1.20f);
            r.featMotionRange = success ? (0.55f + Random.value * 0.40f) : (0.25f + Random.value * 0.50f);
            r.meanConfidence  = success ? (0.65f + Random.value * 0.30f) : (0.45f + Random.value * 0.35f);

            // Use config slightly (optional): if hold_time is high, penalize hold_ratio a bit on failure
            if (!success && cfg != null && cfg.hold_time > 1.8f)
                r.featHoldRatio = Mathf.Clamp01(r.featHoldRatio - 0.08f);

            // robustness
            r.trackingLossCount = Random.Range(0, success ? 2 : 3);
            r.trackingLossDurationSec = Random.value * (success ? 0.25f : 0.60f);
        }
    }
}
