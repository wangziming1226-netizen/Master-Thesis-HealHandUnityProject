using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Thesis.Core
{
    [Serializable]
    public class ThesisSessionLog
    {
        public string session_id;
        public string timestamp_start_utc;
        public string timestamp_end_utc;

        public string mode;              // Practice/Assessment/Daily/Story/LegacyRandom/LegacyCard
        public string fixed_or_adaptive; // fixed/adaptive

        public string app_version;
        public string gesture_set_version;

        public float summary_completion_rate;
        public float summary_mean_quality;
        public float summary_tracking_loss_rate;
    }

    [Serializable]
    public class ThesisTaskLog
    {
        public string task_id;
        public string session_id;
        public string gesture_type;

        // difficulty parameters
        public float difficulty_hold_time;
        public float difficulty_tolerance;
        public float difficulty_rhythm_speed;
        public int difficulty_sequence_len;
        public int difficulty_hint_intensity;

        // outcome
        public bool success;
        public float completion_time_sec;

        // AI outputs
        public int quality_score_0_100;
        public string error_type;

        // features (summary only)
        public float feat_stability;
        public float feat_hold_ratio;
        public float feat_response_time;
        public float feat_motion_range;
        public float mean_confidence;

        // robustness
        public int tracking_loss_count;
        public float tracking_loss_duration_sec;

        public string timestamp_task_start_utc;
        public string timestamp_task_end_utc;
    }

    public class ThesisSessionLogger : MonoBehaviour
    {
        [Header("Session Settings (Smoke Test)")]
        public string mode = "Practice";
        public string fixedOrAdaptive = "fixed";
        public string gestureSetVersion = "gset_v1";

        [Header("Debug")]
        public bool printPathsToConsole = true;

        private ThesisSessionLog _session;
        private readonly List<ThesisTaskLog> _tasks = new List<ThesisTaskLog>();

        public bool HasActiveSession => _session != null && string.IsNullOrEmpty(_session.timestamp_end_utc);

        // ---- Public API (Week1) ----

        [ContextMenu("Start Session")]
        public void StartSession()
        {
            if (HasActiveSession)
            {
                Debug.LogWarning("Session already active. End it before starting a new one.");
                return;
            }

            _tasks.Clear();

            _session = new ThesisSessionLog
            {
                session_id = MakeSessionId(),
                timestamp_start_utc = ExportUtils.IsoNowUtc(),
                timestamp_end_utc = "",
                mode = mode,
                fixed_or_adaptive = fixedOrAdaptive,
                app_version = Application.version,
                gesture_set_version = gestureSetVersion,
                summary_completion_rate = 0f,
                summary_mean_quality = 0f,
                summary_tracking_loss_rate = 0f
            };

            if (printPathsToConsole)
            {
                Debug.Log($"[Thesis] persistentDataPath: {Application.persistentDataPath}");
                Debug.Log($"[Thesis] exportDir: {ExportUtils.GetExportDir()}");
                Debug.Log($"[Thesis] session_id: {_session.session_id}");
            }
        }

        [ContextMenu("Add Fake Task Record")]
        public void AddFakeTaskRecord()
        {
            if (!HasActiveSession)
            {
                Debug.LogWarning("No active session. StartSession first.");
                return;
            }

            // Create a fake task log (replace with real TaskEngine later)
            string tStart = ExportUtils.IsoNowUtc();

            var task = new ThesisTaskLog
            {
                task_id = $"T{_tasks.Count + 1:D3}",
                session_id = _session.session_id,
                gesture_type = "pinch",

                difficulty_hold_time = 1.5f,
                difficulty_tolerance = 0.12f,
                difficulty_rhythm_speed = 1.0f,
                difficulty_sequence_len = 1,
                difficulty_hint_intensity = 2,

                success = UnityEngine.Random.value > 0.2f,
                completion_time_sec = (float)Math.Round(2.5 + UnityEngine.Random.value * 2.0, 2),

                quality_score_0_100 = UnityEngine.Random.Range(45, 90),
                error_type = "none",

                feat_stability = (float)Math.Round(UnityEngine.Random.value, 3),
                feat_hold_ratio = (float)Math.Round(0.7 + UnityEngine.Random.value * 0.3, 3),
                feat_response_time = (float)Math.Round(0.4 + UnityEngine.Random.value * 0.8, 3),
                feat_motion_range = (float)Math.Round(0.5 + UnityEngine.Random.value * 0.5, 3),
                mean_confidence = (float)Math.Round(0.6 + UnityEngine.Random.value * 0.4, 3),

                tracking_loss_count = UnityEngine.Random.Range(0, 2),
                tracking_loss_duration_sec = (float)Math.Round(UnityEngine.Random.value * 0.5, 3),

                timestamp_task_start_utc = tStart,
                timestamp_task_end_utc = ExportUtils.IsoNowUtc()
            };

            if (!task.success)
                task.error_type = "hold_too_short";

            _tasks.Add(task);
            Debug.Log($"[Thesis] Added fake task: {task.task_id}, success={task.success}, score={task.quality_score_0_100}");
        }
        
        public void AddTaskRecord(
            string gestureType,
            bool success,
            float completionTimeSec,
            int qualityScore0To100,
            string errorType,
            float featStability,
            float featHoldRatio,
            float featResponseTime,
            float featMotionRange,
            float meanConfidence,
            int trackingLossCount,
            float trackingLossDurationSec,
            float difficultyHoldTime,
            float difficultyTolerance,
            float difficultyRhythmSpeed,
            int difficultySequenceLen,
            int difficultyHintIntensity
        )
        {
            if (!HasActiveSession)
            {
                Debug.LogWarning("No active session. StartSession first.");
                return;
            }
            
            string tStart = ExportUtils.IsoNowUtc();
            
            var task = new ThesisTaskLog
            {
                task_id = $"T{_tasks.Count + 1:D3}",
                session_id = _session.session_id,
                gesture_type = gestureType,
                
                difficulty_hold_time = difficultyHoldTime,
                difficulty_tolerance = difficultyTolerance,
                difficulty_rhythm_speed = difficultyRhythmSpeed,
                difficulty_sequence_len = difficultySequenceLen,
                difficulty_hint_intensity = difficultyHintIntensity,
                
                success = success,
                completion_time_sec = completionTimeSec,
                
                quality_score_0_100 = qualityScore0To100,
                error_type = errorType,
                
                feat_stability = featStability,
                feat_hold_ratio = featHoldRatio,
                feat_response_time = featResponseTime,
                feat_motion_range = featMotionRange,
                mean_confidence = meanConfidence,
                
                tracking_loss_count = trackingLossCount,
                tracking_loss_duration_sec = trackingLossDurationSec,
                
                timestamp_task_start_utc = tStart,
                timestamp_task_end_utc = ExportUtils.IsoNowUtc()
            };
            
            _tasks.Add(task);
            Debug.Log($"[Thesis] Added task: {task.task_id}, gesture={gestureType}, success={success}, score={qualityScore0To100}");
        }
        

        [ContextMenu("End Session")]
        public void EndSession()
        {
            if (!HasActiveSession)
            {
                Debug.LogWarning("No active session.");
                return;
            }

            _session.timestamp_end_utc = ExportUtils.IsoNowUtc();
            ComputeSessionSummary();
            Debug.Log($"[Thesis] Session ended: {_session.session_id}");
        }

        [ContextMenu("Export Session (JSON + CSV)")]
        public void ExportSession()
        {
            if (_session == null)
            {
                Debug.LogWarning("No session to export. StartSession first.");
                return;
            }

            // If session still active, we allow export but warn.
            if (HasActiveSession)
                Debug.LogWarning("Exporting while session is still active (timestamp_end_utc is empty). Consider EndSession first.");

            string exportDir = ExportUtils.GetExportDir();
            string baseName = _session.session_id;

            // session.json
            string sessionJson = JsonUtility.ToJson(_session, prettyPrint: true);
            string sessionPath = System.IO.Path.Combine(exportDir, $"{baseName}_session.json");
            ExportUtils.WriteTextFile(sessionPath, sessionJson);

            // tasks.csv
            string csvPath = System.IO.Path.Combine(exportDir, $"{baseName}_tasks.csv");
            string csv = BuildTasksCsv();
            ExportUtils.WriteTextFile(csvPath, csv);

            Debug.Log($"[Thesis] Exported:\n- {sessionPath}\n- {csvPath}");
        }

        // ---- Internals ----

        private string MakeSessionId()
        {
            // Example: 2026-02-14T12-34-56Z_5A2F
            string t = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture);
            string rnd = UnityEngine.Random.Range(0, 65535).ToString("X4");
            return $"{t}_{rnd}";
        }

        private void ComputeSessionSummary()
        {
            if (_tasks.Count == 0)
            {
                _session.summary_completion_rate = 0f;
                _session.summary_mean_quality = 0f;
                _session.summary_tracking_loss_rate = 0f;
                return;
            }

            int successCount = 0;
            float scoreSum = 0f;
            float lossDurSum = 0f;
            float totalTaskTime = 0f;

            foreach (var t in _tasks)
            {
                if (t.success) successCount++;
                scoreSum += t.quality_score_0_100;
                lossDurSum += t.tracking_loss_duration_sec;
                totalTaskTime += Mathf.Max(0.001f, t.completion_time_sec);
            }

            _session.summary_completion_rate = (float)successCount / _tasks.Count;
            _session.summary_mean_quality = scoreSum / _tasks.Count;

            // simple proxy: tracking loss duration / total completion time
            _session.summary_tracking_loss_rate = lossDurSum / totalTaskTime;
        }

        private string BuildTasksCsv()
        {
            var headers = new List<string>
            {
                "task_id","session_id","gesture_type",
                "difficulty_hold_time","difficulty_tolerance","difficulty_rhythm_speed","difficulty_sequence_len","difficulty_hint_intensity",
                "success","completion_time_sec",
                "quality_score_0_100","error_type",
                "feat_stability","feat_hold_ratio","feat_response_time","feat_motion_range","mean_confidence",
                "tracking_loss_count","tracking_loss_duration_sec",
                "timestamp_task_start_utc","timestamp_task_end_utc"
            };

            var rows = new List<List<string>>();
            foreach (var t in _tasks)
            {
                rows.Add(new List<string>
                {
                    t.task_id, t.session_id, t.gesture_type,
                    t.difficulty_hold_time.ToString(CultureInfo.InvariantCulture),
                    t.difficulty_tolerance.ToString(CultureInfo.InvariantCulture),
                    t.difficulty_rhythm_speed.ToString(CultureInfo.InvariantCulture),
                    t.difficulty_sequence_len.ToString(CultureInfo.InvariantCulture),
                    t.difficulty_hint_intensity.ToString(CultureInfo.InvariantCulture),
                    t.success ? "1" : "0",
                    t.completion_time_sec.ToString(CultureInfo.InvariantCulture),
                    t.quality_score_0_100.ToString(CultureInfo.InvariantCulture),
                    t.error_type,
                    t.feat_stability.ToString(CultureInfo.InvariantCulture),
                    t.feat_hold_ratio.ToString(CultureInfo.InvariantCulture),
                    t.feat_response_time.ToString(CultureInfo.InvariantCulture),
                    t.feat_motion_range.ToString(CultureInfo.InvariantCulture),
                    t.mean_confidence.ToString(CultureInfo.InvariantCulture),
                    t.tracking_loss_count.ToString(CultureInfo.InvariantCulture),
                    t.tracking_loss_duration_sec.ToString(CultureInfo.InvariantCulture),
                    t.timestamp_task_start_utc,
                    t.timestamp_task_end_utc
                });
            }

            return ExportUtils.ToCsv(headers, rows);
        }
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            
            
        }

    }
}
