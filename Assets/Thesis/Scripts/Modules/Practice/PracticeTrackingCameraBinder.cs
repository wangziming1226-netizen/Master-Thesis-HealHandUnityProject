using UnityEngine;
using System.Collections;

namespace Thesis.Core
{
    public class PracticeTrackingCameraBinder : MonoBehaviour
    {
        private IEnumerator Start()
        {
            for (int i = 0; i < 60; i++)
            {
                if (TryBind())
                    yield break;

                yield return null;
            }

            Debug.LogWarning("[PracticeTrackingCameraBinder] Failed to bind after 60 frames.");
        }

        private bool TryBind()
        {
            var allTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            Transform trackingRoot = null;
            foreach (var t in allTransforms)
            {
                if (t != null && t.name == "TrackingServicesRoot")
                {
                    trackingRoot = t;
                    break;
                }
            }

            if (trackingRoot == null)
            {
                Debug.Log("[PracticeTrackingCameraBinder] TrackingServicesRoot not found yet.");
                return false;
            }

            Transform trackingCanvasTf = null;
            foreach (var t in trackingRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && t.name == "TrackingCanvas")
                {
                    trackingCanvasTf = t;
                    break;
                }
            }

            if (trackingCanvasTf == null)
            {
                Debug.Log("[PracticeTrackingCameraBinder] TrackingCanvas not found under TrackingServicesRoot yet.");
                return false;
            }

            var canvas = trackingCanvasTf.GetComponent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[PracticeTrackingCameraBinder] TrackingCanvas has no Canvas component.");
                return false;
            }

            Debug.Log($"[PracticeTrackingCameraBinder] Found canvas={canvas.name}, mode={canvas.renderMode}");

            if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                Debug.LogWarning("[PracticeTrackingCameraBinder] TrackingCanvas is not in ScreenSpaceCamera mode.");
                return false;
            }

            if (Camera.main == null)
            {
                Debug.Log("[PracticeTrackingCameraBinder] Camera.main not ready yet.");
                return false;
            }

            canvas.worldCamera = Camera.main;
            Debug.Log($"[PracticeTrackingCameraBinder] Bound {canvas.name} to {Camera.main.name}");
            return true;
        }
    }
}