using UnityEngine;
using UnityEngine.UI;
using Thesis.Core;

namespace Thesis.UI
{
    public class ThesisHubController : MonoBehaviour
    {
        [Header("References")]
        public ThesisSessionLogger logger;

        [Header("Hub Buttons (drag from Hierarchy)")]
        public Button btnStart;      // Assign the Start button from the Hierarchy here.
        public Button btnAddFake;    // Assign the AddFake button here.
        public Button btnEnd;        // Assign the End button here.
        public Button btnExport;     // Assign the Export button here.

        private void Awake()
        {
            // The logger can be assigned automatically. If it is not found in the scene, show an error.
            if (logger == null)
                logger = FindFirstObjectByType<ThesisSessionLogger>();

            if (logger == null)
            {
                Debug.LogError("[Hub] ThesisSessionLogger not found. Please ensure ThesisServices exists in ThesisHub scene (DontDestroyOnLoad).");
            }

            UpdateUI();
        }

        private void OnEnable()
        {
            UpdateUI();
        }

        // ====== Keep these method names unchanged so existing OnClick bindings still work. ======
        public void StartSession()
        {
            if (!EnsureLogger()) return;

            // Prevent accidental duplicate starts when a session is already active.
            if (logger.HasActiveSession)
            {
                Debug.LogWarning("[Hub] Session already active. Use End first.");
                UpdateUI();
                return;
            }

            logger.StartSession();
            UpdateUI();
        }

        public void AddFakeTask()
        {
            if (!EnsureLogger()) return;

            if (!logger.HasActiveSession)
            {
                Debug.LogWarning("[Hub] No active session. Click Start first.");
                UpdateUI();
                return;
            }

            logger.AddFakeTaskRecord();
            UpdateUI();
        }

        public void EndSession()
        {
            if (!EnsureLogger()) return;

            if (!logger.HasActiveSession)
            {
                Debug.LogWarning("[Hub] No active session to end.");
                UpdateUI();
                return;
            }

            logger.EndSession();
            UpdateUI();
        }

        public void Export()
        {
            if (!EnsureLogger()) return;

            // This is intentionally permissive. It can be changed to require End before Export if needed.
            logger.ExportSession();
            UpdateUI();
        }

        // ====== Keep the button states in sync to avoid accidental actions. ======
        private void UpdateUI()
        {
            bool hasLogger = (logger != null);
            bool active = hasLogger && logger.HasActiveSession;

            // Disable every button if the logger is missing, so the UI does not appear unresponsive.
            if (btnStart)   btnStart.interactable   = hasLogger && !active;
            if (btnAddFake) btnAddFake.interactable = hasLogger && active;
            if (btnEnd)     btnEnd.interactable     = hasLogger && active;

            // Export is available only after the session has ended.
            // Change this to hasLogger if exporting during an active session should be allowed.
            if (btnExport)  btnExport.interactable  = hasLogger && !active;
        }

        private bool EnsureLogger()
        {
            if (logger != null) return true;

            logger = FindFirstObjectByType<ThesisSessionLogger>();
            if (logger == null)
            {
                Debug.LogError("[Hub] ThesisSessionLogger is null. Cannot proceed.");
                UpdateUI();
                return false;
            }

            UpdateUI();
            return true;
        }
    }
}
