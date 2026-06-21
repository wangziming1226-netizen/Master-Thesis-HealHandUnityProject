using UnityEngine;
using UnityEngine.UI;

namespace Thesis.Core
{
    public class HubController : MonoBehaviour
    {
        [Header("Refs")]
        public SceneLoader sceneLoader;

        [Header("Hub Buttons")]
        public Button btnStart;
        public Button btnAddFake;
        public Button btnEnd;
        public Button btnExport;

        private IHubModeRunner _runner;

        private void Awake()
        {
            if (sceneLoader == null)
                sceneLoader = FindFirstObjectByType<SceneLoader>();

            if (sceneLoader != null)
            {
                sceneLoader.OnModeSceneLoaded += _ => RefreshRunner();
                sceneLoader.OnModeSceneUnloaded += () => { _runner = null; RefreshButtons(); };
            }

            RefreshRunner();
        }

        // ========== Mode Select ==========
        public void SelectDaily() => sceneLoader.LoadDaily();
        public void SelectPractice() => sceneLoader.LoadPractice();
        public void SelectStory() => sceneLoader.LoadStory();
        public void SelectAssessment() => sceneLoader.LoadAssessment();

        // ========== Hub Actions ==========
        public void StartSession()
        {
            if (_runner == null) { Debug.LogWarning("[Hub] No mode runner loaded."); return; }
            _runner.HubStart();
            RefreshButtons();
        }

        public void AddFake()
        {
            if (_runner == null) { Debug.LogWarning("[Hub] No mode runner loaded."); return; }
            _runner.HubAddFake();
            RefreshButtons();
        }

        public void EndSession()
        {
            if (_runner == null) { Debug.LogWarning("[Hub] No mode runner loaded."); return; }
            _runner.HubEnd();
            RefreshButtons();
        }

        public void ExportAndBackToHub()
        {
            if (_runner == null) { Debug.LogWarning("[Hub] No mode runner loaded."); return; }
            _runner.HubExport();
            RefreshButtons();

            // Return to the Hub after exporting by unloading the current mode scene.
            sceneLoader.UnloadCurrentModeScene();
        }

        // ========== Internals ==========
        private void RefreshRunner()
        {
            // Find a component that implements IHubModeRunner across all loaded scenes.
            var monos = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            _runner = null;
            foreach (var m in monos)
            {
                if (m is IHubModeRunner r)
                {
                    _runner = r;
                    break;
                }
            }
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool hasMode = _runner != null;
            bool active = hasMode && _runner.HasActiveSession;

            if (btnStart)   btnStart.interactable   = hasMode && !active;
            if (btnAddFake) btnAddFake.interactable = hasMode && active;
            if (btnEnd)     btnEnd.interactable     = hasMode && active;
            if (btnExport)  btnExport.interactable  = hasMode && !active; // Export is only available after End to prevent accidental actions.
        }
    }
}
