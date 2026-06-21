using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Thesis.Core
{
    [RequireComponent(typeof(Canvas))]
    public class TrackingCanvasCameraBinder : MonoBehaviour
    {
        private Canvas _canvas;
        private Coroutine _bindRoutine;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartBindRoutine();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_bindRoutine != null)
                StopCoroutine(_bindRoutine);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartBindRoutine();
        }

        private void StartBindRoutine()
        {
            if (_bindRoutine != null)
                StopCoroutine(_bindRoutine);

            _bindRoutine = StartCoroutine(BindForSeveralFrames());
        }

        private IEnumerator BindForSeveralFrames()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                if (RebindCamera())
                    yield break;
            }

            Debug.LogWarning("[TrackingCanvasCameraBinder] Failed to bind camera after 10 frames.");
        }

        private bool RebindCamera()
        {
            if (_canvas == null)
                _canvas = GetComponent<Canvas>();

            if (_canvas == null || _canvas.renderMode != RenderMode.ScreenSpaceCamera)
                return false;

            Camera cam = Camera.main;

            if (cam == null)
            {
                var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (cameras != null && cameras.Length > 0)
                    cam = cameras[0];
            }

            if (cam == null)
            {
                Debug.LogWarning("[TrackingCanvasCameraBinder] No camera found to bind.");
                return false;
            }

            _canvas.worldCamera = cam;
            Debug.Log($"[TrackingCanvasCameraBinder] Bound camera: {cam.name}");
            return true;
        }
    }
}