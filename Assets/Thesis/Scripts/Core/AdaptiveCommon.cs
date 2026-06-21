using System;

namespace Thesis.Core.Adaptive
{
    [Serializable]
    public enum AdaptiveErrorType
    {
        None,
        TrackingLost,
        GestureMismatch,
        UnstableHold,
        Timeout
    }

    [Serializable]
    public class AdaptiveTaskResult
    {
        public string moduleName;          // Practice / Assessment / Daily
        public string gestureType;
        public bool success;
        public float score;
        public AdaptiveErrorType errorType;
        public float trackingRatio;
        public float strongMatchRatio;
        public float completionTimeSec;
        public string timestampUtc;
    }

    [Serializable]
    public struct AdaptiveParams
    {
        public float holdSeconds;
        public float strongThreshold;
        public float weakThreshold;

        public AdaptiveParams(float hold, float strong, float weak)
        {
            holdSeconds = hold;
            strongThreshold = strong;
            weakThreshold = weak;
        }
    }
}