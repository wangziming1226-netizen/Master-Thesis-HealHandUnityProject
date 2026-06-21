using UnityEngine;
using System.Collections;

namespace Thesis.UI
{
    [RequireComponent(typeof(Canvas))]
    public class PracticeCanvasBindToTrackingCamera : MonoBehaviour
    {
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private IEnumerator Start()
        {

            for (int i = 0; i < 60; i++)
            {
                if (TryBind())
                    yield break;

                yield return null;
            }

            Debug.LogWarning("[PracticeCanvasBindToTrackingCamera] Failed to bind TrackingCamera after 60 frames.");
        }

        private bool TryBind()
        {
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            if (_canvas == null)
                return false;

            if (_canvas.renderMode != RenderMode.ScreenSpaceCamera)
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;

            Camera trackingCam = null;
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var cam in cameras)
            {
                if (cam != null && cam.name == "TrackingCamera")
                {
                    trackingCam = cam;
                    break;
                }
            }

            if (trackingCam == null)
                return false;

            _canvas.worldCamera = trackingCam;
            Debug.Log("[PracticeCanvasBindToTrackingCamera] Bound Practice Canvas to TrackingCamera.");
            return true;
        }
    }
}