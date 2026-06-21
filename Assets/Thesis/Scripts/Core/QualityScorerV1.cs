using UnityEngine;

namespace Thesis.Core
{
    public static class QualityScorerV1
    {
        // Clamp helper
        private static float Clamp01(float v) => Mathf.Clamp01(v);

        // Main API
        public static (int score0To100, string errorType) Score(
            float featStability,     // 0..1 (higher better)
            float featHoldRatio,     // 0..1 (>=1 means met/over met)
            float featResponseTime,  // seconds (lower better)
            float featMotionRange,   // 0..1 (higher better)
            float meanConfidence,    // 0..1 (higher better)
            int trackingLossCount,   // >=0
            float trackingLossDurationSec, // seconds
            float completionTimeSec  // seconds
        )
        {
            // --- Normalise inputs into 0..1 "goodness" ---
            float stabilityGood = Clamp01(featStability);                // assume already 0..1
            float holdGood = Clamp01(featHoldRatio);                     // cap to 1
            float rangeGood = Clamp01(featMotionRange);
            float confGood = Clamp01(meanConfidence);

            // Response time: map [0.2s .. 2.0s] to goodness [1 .. 0]
            float responseGood = 1f - Clamp01((featResponseTime - 0.2f) / (2.0f - 0.2f));

            // Tracking loss penalty (duration ratio wrt completion time)
            float denom = Mathf.Max(0.001f, completionTimeSec);
            float lossRatio = Clamp01(trackingLossDurationSec / denom); // 0..1
            float lossPenalty = lossRatio; // higher loss ratio -> worse

            // Extra penalty for frequent dropouts (light)
            float lossCountPenalty = Clamp01(trackingLossCount / 5f); // 5+ dropouts => full penalty

            // --- Weighted score (interpretable) ---
            // weights sum approx 1, then apply penalties
            float baseScore01 =
                0.25f * stabilityGood +
                0.30f * holdGood +
                0.15f * rangeGood +
                0.15f * responseGood +
                0.15f * confGood;

            // Apply penalties (keep bounded)
            float score01 = baseScore01
                            - 0.25f * lossPenalty
                            - 0.10f * lossCountPenalty;

            score01 = Mathf.Clamp01(score01);

            int score0To100 = Mathf.RoundToInt(score01 * 100f);

            // --- Error type rules (explainable) ---
            // priority: hold -> tracking -> confidence -> stability -> response
            string errorType = "none";

            if (featHoldRatio < 0.85f)
                errorType = "hold_too_short";
            else if (lossRatio > 0.20f || trackingLossCount >= 2)
                errorType = "tracking_loss";
            else if (meanConfidence < 0.60f)
                errorType = "low_confidence";
            else if (featStability < 0.45f)
                errorType = "low_stability";
            else if (featResponseTime > 1.2f)
                errorType = "slow_response";

            return (score0To100, errorType);
        }
    }
}
