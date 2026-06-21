using UnityEngine;

namespace Thesis.Core
{
    public class ThesisGestureInput : MonoBehaviour
    {
        [Header("Runtime")]
        public string currentGesture = "none"; // "ok" / "open_hand" / "fist" / "thumbs_up" / "none"
        public float lastSeenTime;
        public float confidence = 1f;

        [Header("Tuning")]
        [Tooltip("How long after last event we still consider tracking present.")]
        public float trackingTimeoutSeconds = 0.5f;

        [Header("Debug")]
        public bool debugLogs = false;

        public bool IsTracking => (Time.time - lastSeenTime) < trackingTimeoutSeconds;

        private void SetGesture(string g)
        {
            currentGesture = g;
            lastSeenTime = Time.time;

            if (debugLogs)
                Debug.Log($"[ThesisGestureInput] SetGesture={g}, t={lastSeenTime:0.00}, obj={name}, scene={gameObject.scene.name}");
        }

        public void OnOk()
        {
            SetGesture("ok");
        }

        public void OnOpenHand()
        {
            SetGesture("open_hand");
        }

        public void OnFist()
        {
            SetGesture("fist");
        }

        public void OnThumbsUp()
        {
            SetGesture("thumbs_up");
        }

        public void ClearGesture()
        {
            currentGesture = "none";

            if (debugLogs)
                Debug.Log($"[ThesisGestureInput] ClearGesture, obj={name}, scene={gameObject.scene.name}");
        }
    }
}