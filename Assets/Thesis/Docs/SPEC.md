# HealHand Thesis Mainline Spec (Week 1)

## Scope
**In scope (thesis mainline):**
- Avatar-based privacy UI (no camera preview on screen).
- Practice / Assessment / Daily / Story modules as the primary training experience.
- Movement quality scoring (0–100) + simple error cues.
- Adaptive difficulty (background personalisation, no tutor character).
- Unified session + task logging and one-click export (CSV/JSON).
- Evaluation protocol for fixed vs adaptive difficulty.

**Out of scope (for evaluation):**
- Legacy Card/Random modes remain in the app but are **not** used for controlled experiments or dissertation figures.

## Research Questions + Success Criteria
**RQ1 (Privacy):** Can we provide a usable rehab training interface without showing/storing camera video?
- Success: no raw frames displayed or stored; exports contain anonymised summaries only.

**RQ2 (Scoring):** Can we compute an interpretable movement quality score (0–100) and error cues?
- Success: score and error_type are logged per task; can visualise trends across sessions.

**RQ3 (Adaptation):** Does adaptive difficulty outperform fixed difficulty?
- Success: compare fixed vs adaptive using completion rate, time, quality score trends, failure streaks, tracking loss metrics.

## Architecture / Module Boundaries
- **TrackingProvider**: provides hand landmarks + confidence + tracking loss events (no video output).
- **TaskEngine**: defines tasks, state machine, success/fail judgement, extracts summary features.
- **QualityScorer**: converts summary features to quality_score_0_100 + error_type.
- **DifficultyController**: updates difficulty parameters with guardrails.
- **SessionLogger/Exporter**: writes session.json + tasks.csv to persistentDataPath.

## Privacy Constraints
- No camera frames are displayed or stored.
- No per-frame landmark sequences are exported.
- Only task-level anonymised summaries and performance statistics are stored/exported.
