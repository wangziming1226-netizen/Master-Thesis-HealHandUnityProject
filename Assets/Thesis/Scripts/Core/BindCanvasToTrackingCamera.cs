using UnityEngine;
using System.Collections;

namespace Thesis.UI
{
    [RequireComponent(typeof(Canvas))]
    public class BindCanvasToTrackingCamera : MonoBehaviour
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
                var cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var cam in cams)
                {
                    if (cam != null && cam.name == "TrackingCamera")
                    {
                        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        _canvas.worldCamera = cam;
                        Debug.Log("[BindCanvasToTrackingCamera] Bound to TrackingCamera.");
                        yield break;
                    }
                }
                yield return null;
            }

            Debug.LogWarning("[BindCanvasToTrackingCamera] TrackingCamera not found.");
        }
    }
}