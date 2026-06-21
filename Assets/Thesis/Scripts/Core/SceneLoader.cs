using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

namespace Thesis.Core
{
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene Names (must match Build Settings)")]
        public string dailyScene = "Daily";
        public string practiceScene = "Practice";
        public string storyScene = "Story";
        public string assessmentScene = "Assessment";

        [Header("Hub Scene Name")]
        [Tooltip("Name of the hub scene. We prefer to keep its EventSystem enabled.")]
        public string hubScene = "ThesisHub";

        [Header("EventSystem Handling")]
        [Tooltip("If true, tries to ensure exactly ONE active EventSystem at runtime (preferred).")]
        public bool enforceSingleEventSystem = true;

        [Tooltip("If true, after loading a mode scene we set it as the active scene.")]
        public bool setModeSceneActive = true;

        private string _loadedModeSceneName = null;
        private bool _isTransitioning = false;

        public event Action<string> OnModeSceneLoaded;
        public event Action OnModeSceneUnloaded;

        public string LoadedModeSceneName => _loadedModeSceneName;
        public bool IsTransitioning => _isTransitioning;

        public void LoadDaily() => LoadModeScene(dailyScene);
        public void LoadPractice() => LoadModeScene(practiceScene);
        public void LoadStory() => LoadModeScene(storyScene);
        public void LoadAssessment() => LoadModeScene(assessmentScene);

        public void UnloadCurrentModeScene()
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[SceneLoader] Transition in progress, ignoring unload request.");
                return;
            }

            if (string.IsNullOrEmpty(_loadedModeSceneName))
            {
                OnModeSceneUnloaded?.Invoke();
                return;
            }

            var modeScene = SceneManager.GetSceneByName(_loadedModeSceneName);
            if (!modeScene.isLoaded)
            {
                _loadedModeSceneName = null;
                OnModeSceneUnloaded?.Invoke();
                return;
            }

            _isTransitioning = true;

            SceneManager.UnloadSceneAsync(_loadedModeSceneName).completed += _ =>
            {
                _loadedModeSceneName = null;

                // Return active scene to hub for stability.
                var hub = SceneManager.GetSceneByName(hubScene);
                if (hub.isLoaded)
                    SceneManager.SetActiveScene(hub);

                // Re-enforce EventSystem after unload (important if the only enabled one was in mode scene)
                if (enforceSingleEventSystem)
                    EnsureSingleEventSystem(preferSceneName: hubScene);

                _isTransitioning = false;
                OnModeSceneUnloaded?.Invoke();
            };
        }

        private void LoadModeScene(string sceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[SceneLoader] Transition in progress, ignoring load request.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] sceneName is empty.");
                return;
            }

            // Already loaded this mode scene
            if (_loadedModeSceneName == sceneName && SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                var s = SceneManager.GetSceneByName(sceneName);
                if (s.isLoaded && setModeSceneActive)
                    SceneManager.SetActiveScene(s);

                if (enforceSingleEventSystem)
                    EnsureSingleEventSystem(preferSceneName: hubScene);

                OnModeSceneLoaded?.Invoke(sceneName);
                return;
            }

            _isTransitioning = true;

            // Unload old then load new
            if (!string.IsNullOrEmpty(_loadedModeSceneName))
            {
                var old = _loadedModeSceneName;
                var oldScene = SceneManager.GetSceneByName(old);

                if (oldScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(old).completed += _ => LoadAdditive(sceneName);
                }
                else
                {
                    _loadedModeSceneName = null;
                    LoadAdditive(sceneName);
                }
            }
            else
            {
                LoadAdditive(sceneName);
            }
        }

        private void LoadAdditive(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] LoadSceneAsync failed: {sceneName}");
                _isTransitioning = false;
                return;
            }

            op.completed += _ =>
            {
                _loadedModeSceneName = sceneName;

                var modeScene = SceneManager.GetSceneByName(sceneName);
                if (modeScene.isLoaded && setModeSceneActive)
                    SceneManager.SetActiveScene(modeScene);

                // Handle duplicate EventSystems safely.
                if (enforceSingleEventSystem)
                    EnsureSingleEventSystem(preferSceneName: hubScene);

                _isTransitioning = false;
                OnModeSceneLoaded?.Invoke(sceneName);
            };
        }

        /// Ensures there is exactly ONE enabled EventSystem.
        /// Preference: keep the EventSystem that belongs to preferSceneName enabled if possible.
        /// Fallback: if we accidentally disabled all, re-enable one.
        private void EnsureSingleEventSystem(string preferSceneName)
        {
            var all = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (all == null || all.Length == 0) return;
            if (all.Length == 1)
            {
                // Ensure current exists
                if (!all[0].enabled) all[0].enabled = true;
                if (EventSystem.current == null) EventSystem.current = all[0];
                return;
            }

            // 1) Choose preferred EventSystem
            EventSystem preferred = null;
            if (!string.IsNullOrEmpty(preferSceneName))
            {
                var preferScene = SceneManager.GetSceneByName(preferSceneName);
                if (preferScene.isLoaded)
                {
                    foreach (var es in all)
                    {
                        if (es != null && es.gameObject.scene == preferScene)
                        {
                            preferred = es;
                            break;
                        }
                    }
                }
            }

            // 2) If no preferred found, pick the first enabled, otherwise first.
            if (preferred == null)
            {
                foreach (var es in all)
                {
                    if (es != null && es.enabled)
                    {
                        preferred = es;
                        break;
                    }
                }
                if (preferred == null) preferred = all[0];
            }

            // 3) Disable all others, keep preferred enabled
            int enabledCountBefore = 0;
            foreach (var es in all)
            {
                if (es != null && es.enabled) enabledCountBefore++;
            }

            foreach (var es in all)
            {
                if (es == null) continue;

                bool shouldEnable = (es == preferred);
                if (es.enabled != shouldEnable)
                    es.enabled = shouldEnable;

                // Also disable modules on disabled ones to avoid any input conflicts
                if (!shouldEnable)
                {
                    var standalone = es.GetComponent<StandaloneInputModule>();
                    if (standalone != null) standalone.enabled = false;

#if ENABLE_INPUT_SYSTEM
                    var inputSystem = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    if (inputSystem != null) inputSystem.enabled = false;
#endif
                }
                else
                {
                    // Ensure preferred has a module enabled if present
                    var standalone = es.GetComponent<StandaloneInputModule>();
                    if (standalone != null) standalone.enabled = true;

#if ENABLE_INPUT_SYSTEM
                    var inputSystem = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    if (inputSystem != null) inputSystem.enabled = true;
#endif
                }
            }

            // 4) Safety: if somehow all got disabled, re-enable preferred
            if (preferred != null && !preferred.enabled)
                preferred.enabled = true;

            if (EventSystem.current == null || EventSystem.current != preferred)
                EventSystem.current = preferred;

            // Optional debug
            // Debug.Log($"[SceneLoader] EventSystems before={enabledCountBefore}, kept={preferred.gameObject.name} ({preferred.gameObject.scene.name})");
        }
    }
}