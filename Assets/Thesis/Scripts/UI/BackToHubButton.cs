using UnityEngine;
using Thesis.UI;

namespace Thesis.UI
{
    public class BackToHubButton : MonoBehaviour
    {
        public void Back()
        {
            var loader = FindFirstObjectByType<HubSceneLoader>();
            if (loader == null)
            {
                Debug.LogError("[Back] HubSceneLoader not found in this scene. Add one (e.g., DailyServices) or attach it somewhere.");
                return;
            }

            loader.LoadHub(); // or loader.BackToHub();
        }
    }
}
