using System;
using UnityEngine;

namespace Thesis.Core
{
    [Serializable]
    public class TaskResult
    {
        // outcome
        public bool success;
        public float completionTimeSec;

        // features (summary only)
        public float featStability;
        public float featHoldRatio;
        public float featResponseTime;
        public float featMotionRange;
        public float meanConfidence;

        // robustness
        public int trackingLossCount;
        public float trackingLossDurationSec;

        // timestamps (UTC ISO)
        public string timestampTaskStartUtc;
        public string timestampTaskEndUtc;
    }
}
