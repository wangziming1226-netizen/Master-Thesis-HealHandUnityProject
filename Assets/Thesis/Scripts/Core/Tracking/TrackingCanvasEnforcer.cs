using UnityEngine;

namespace Thesis.Core
{
    [RequireComponent(typeof(Canvas))]
    public class TrackingCanvasEnforcer : MonoBehaviour
    {
        private Canvas _canvas;
        private bool _logged;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            if (_canvas == null) _canvas = GetComponent<Canvas>();
            if (_canvas == null) return;

            // Enforce the required render mode.
            if (_canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            // Always bind the current main camera.
            if (Camera.main != null && _canvas.worldCamera != Camera.main)
            {
                _canvas.worldCamera = Camera.main;
            }

            // Log this once to confirm which object is being affected.
            if (!_logged)
            {
                Debug.Log(
                    $"[TrackingCanvasEnforcer] path={GetPath(transform)} | id={gameObject.GetInstanceID()} | mode={_canvas.renderMode} | cam={(_canvas.worldCamera ? _canvas.worldCamera.name : "null")}"
                );
                _logged = true;
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
