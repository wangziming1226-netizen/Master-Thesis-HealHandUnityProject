using UnityEngine;

namespace Thesis.UI
{
    public class ModePanelSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject hubHomePanel;
        [SerializeField] private GameObject practicePanel;
        [SerializeField] private GameObject dailyPanel;
        [SerializeField] private GameObject assessmentPanel;
        [SerializeField] private GameObject storyPanel;

        public void ShowHubHome()
        {
            Debug.Log("[ModePanelSwitcher] ShowHubHome");

            if (hubHomePanel != null) hubHomePanel.SetActive(true);
            if (practicePanel != null) practicePanel.SetActive(false);
            if (dailyPanel != null) dailyPanel.SetActive(false);
            if (assessmentPanel != null) assessmentPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
        }

        public void ShowPractice()
        {
            Debug.Log("[ModePanelSwitcher] ShowPractice");

            if (hubHomePanel != null) hubHomePanel.SetActive(false);
            if (practicePanel != null) practicePanel.SetActive(true);
            if (dailyPanel != null) dailyPanel.SetActive(false);
            if (assessmentPanel != null) assessmentPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
        }

        public void ShowDaily()
        {
            Debug.Log("[ModePanelSwitcher] ShowDaily");

            if (hubHomePanel != null) hubHomePanel.SetActive(false);
            if (practicePanel != null) practicePanel.SetActive(false);
            if (dailyPanel != null) dailyPanel.SetActive(true);
            if (assessmentPanel != null) assessmentPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
        }

        public void ShowAssessment()
        {
            Debug.Log("[ModePanelSwitcher] ShowAssessment");

            if (hubHomePanel != null) hubHomePanel.SetActive(false);
            if (practicePanel != null) practicePanel.SetActive(false);
            if (dailyPanel != null) dailyPanel.SetActive(false);
            if (assessmentPanel != null) assessmentPanel.SetActive(true);
            if (storyPanel != null) storyPanel.SetActive(false);
        }

        public void ShowStory()
        {
            Debug.Log("[ModePanelSwitcher] ShowStory");

            if (hubHomePanel != null) hubHomePanel.SetActive(false);
            if (practicePanel != null) practicePanel.SetActive(false);
            if (dailyPanel != null) dailyPanel.SetActive(false);
            if (assessmentPanel != null) assessmentPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(true);
        }

        public void QuitApp()
        {
            Debug.Log("[ModePanelSwitcher] QuitApp");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}