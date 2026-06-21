using UnityEngine;

namespace Thesis.Core.Adaptive
{
    public abstract class AdaptiveModeAdapterBase : MonoBehaviour
    {
        public abstract string ModeName { get; }
        public abstract bool IsModeActive { get; }

        public abstract AdaptiveParams GetCurrentParams();
        public abstract void ApplyAdaptiveParams(AdaptiveParams p);

        public abstract void SetAdaptiveMode(bool enabled);
    }
}