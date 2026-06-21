# Logging Schema v1 (Frozen)

This schema is frozen at Week 1. Future updates may **add fields only** and must not rename or change the meaning of existing fields.

## Export Location
- Directory: Application.persistentDataPath/ThesisExports
- Files per session:
  - <session_id>_session.json
  - <session_id>_tasks.csv

## session.json (Session-level)
| Field | Type | Meaning |
|---|---|---|
| session_id | string | unique session identifier |
| timestamp_start_utc | string | ISO time when session starts |
| timestamp_end_utc | string | ISO time when session ends |
| mode | string | Practice / Assessment / Daily / Story / LegacyRandom / LegacyCard |
| fixed_or_adaptive | string | fixed / adaptive |
| app_version | string | Application.version |
| gesture_set_version | string | version tag for gesture config |
| summary_completion_rate | float | success_count / task_count |
| summary_mean_quality | float | mean quality score in this session |
| summary_tracking_loss_rate | float | tracking loss duration / total completion time (proxy) |

## tasks.csv (Task-level)
> Column names must match exactly.

| Column | Type | Meaning |
|---|---|---|
| task_id | string | task index within session |
| session_id | string | link to session.json |
| gesture_type | string | gesture label (e.g., pinch) |
| difficulty_hold_time | float | hold time requirement |
| difficulty_tolerance | float | tolerance threshold |
| difficulty_rhythm_speed | float | speed parameter |
| difficulty_sequence_len | int | sequence length |
| difficulty_hint_intensity | int | hint intensity level |
| success | int(0/1) | task success flag |
| completion_time_sec | float | completion time in seconds |
| quality_score_0_100 | int | movement quality score |
| error_type | string | error cue category |
| feat_stability | float | summary stability feature |
| feat_hold_ratio | float | hold achieved / hold required |
| feat_response_time | float | response time feature |
| feat_motion_range | float | motion range feature |
| mean_confidence | float | mean tracking confidence |
| tracking_loss_count | int | number of tracking loss events |
| tracking_loss_duration_sec | float | total tracking loss duration |
| timestamp_task_start_utc | string | task start time |
| timestamp_task_end_utc | string | task end time |
