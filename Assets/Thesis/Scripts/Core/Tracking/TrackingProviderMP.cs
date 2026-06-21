using UnityEngine;

namespace Thesis.Core
{
    public class TrackingProviderMP : MonoBehaviour
    {
        public bool IsTracking { get; private set; }
        public float MeanConfidence { get; private set; }
        public Vector3[] Landmarks { get; private set; } = new Vector3[21];

        public int TrackingLossCount { get; private set; }
        public float TrackingLossDurationSec { get; private set; }

        private bool _prevTracking;
        private float _lossStartT;

        private void Update()
        {
            IsTracking = false;
            MeanConfidence = 0f;

            // tracking loss bookkeeping
            if (_prevTracking && !IsTracking)
            {
                TrackingLossCount += 1;
                _lossStartT = Time.realtimeSinceStartup;
            }
            if (!_prevTracking && !IsTracking)
            {
                // still lost
                TrackingLossDurationSec += Time.deltaTime;
            }
            _prevTracking = IsTracking;
        }
    }
}
