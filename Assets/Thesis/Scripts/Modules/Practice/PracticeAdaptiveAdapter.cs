using UnityEngine;
using Thesis.Core.Adaptive;

namespace Thesis.Modules.Practice
{
    public class PracticeAdaptiveAdapter : AdaptiveModeAdapterBase
    {
        public PracticeModeController controller;

        public override string ModeName => "Practice";

        public override bool IsModeActive
        {
            get
            {
                return controller != null && controller.gameObject.activeInHierarchy;
            }
        }

        public override AdaptiveParams GetCurrentParams()
        {
            if (controller == null) return new AdaptiveParams(3f, 0.60f, 0.45f);

            return new AdaptiveParams(
                controller.targetHoldSeconds,
                controller.strongMatchThreshold,
                controller.weakMatchThreshold
            );
        }

        public override void ApplyAdaptiveParams(AdaptiveParams p)
        {
            if (controller == null) return;

            controller.targetHoldSeconds = p.holdSeconds;
            controller.strongMatchThreshold = p.strongThreshold;
            controller.weakMatchThreshold = p.weakThreshold;
        }

        public override void SetAdaptiveMode(bool enabled)
        {
            if (controller == null) return;
            controller.SetAdaptiveMode(enabled);
        }
    }
}