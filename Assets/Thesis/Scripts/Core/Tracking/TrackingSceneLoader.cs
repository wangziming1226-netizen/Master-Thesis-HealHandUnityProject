using UnityEngine;
using UnityEngine.SceneManagement;

namespace Thesis.Core
{
    public class TrackingSceneLoader : MonoBehaviour
    {
        [Tooltip("Must match the scene name in Build Settings")]
        public string trackingSceneName = "Tracking";

        private static bool _loaded;
        private static bool _loading;

        private void Awake()
        {
            EnsureTrackingLoaded();
        }

        public void EnsureTrackingLoaded()
        {
            if (string.IsNullOrEmpty(trackingSceneName))
            {
                Debug.LogError("[TrackingSceneLoader] trackingSceneName is empty.");
                return;
            }

            if (_loaded)
                return;

            var scene = SceneManager.GetSceneByName(trackingSceneName);
            if (scene.isLoaded)
            {
                _loaded = true;
                _loading = false;
                return;
            }

            if (_loading)
                return;

            var op = SceneManager.LoadSceneAsync(trackingSceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[TrackingSceneLoader] Failed to load scene: {trackingSceneName}");
                return;
            }

            _loading = true;

            op.completed += _ =>
            {
                _loading = false;
                _loaded = SceneManager.GetSceneByName(trackingSceneName).isLoaded;

                Debug.Log($"[TrackingSceneLoader] Tracking loaded = {_loaded}");
            };
        }
    }
}