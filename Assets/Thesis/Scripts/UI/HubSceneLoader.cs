using UnityEngine;
using UnityEngine.SceneManagement;

namespace Thesis.UI
{
    public class HubSceneLoader : MonoBehaviour
    {
        [Header("Scene Names (must match Build Settings)")]
        [SerializeField] private string hubScene = "ThesisHub";
        [SerializeField] private string practiceScene = "Practice";
        [SerializeField] private string storyScene = "Story";
        [SerializeField] private string dailyScene = "Daily";
        [SerializeField] private string assessmentScene = "Assessment";

        public void LoadPractice() => LoadAdditive(practiceScene);
        public void LoadStory() => LoadAdditive(storyScene);
        public void LoadDaily() => LoadAdditive(dailyScene);
        public void LoadAssessment() => LoadAdditive(assessmentScene);

        public void LoadHub() => LoadSingle(hubScene);

        // Kept for compatibility with the method name used in BackToHubButton.cs.
        public void BackToHub() => LoadSingle(hubScene);

        private void LoadSingle(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[HubSceneLoader] sceneName is empty.");
                return;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        private void LoadAdditive(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[HubSceneLoader] sceneName is empty.");
                return;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            if (scene.isLoaded)
            {
                Debug.Log($"[HubSceneLoader] Scene already loaded: {sceneName}");
                return;
            }

            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        }
    }
}