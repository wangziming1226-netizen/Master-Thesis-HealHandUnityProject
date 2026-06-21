using UnityEngine;

namespace Thesis.Core
{
    public class TrackingServicesBootstrap : MonoBehaviour
    {
        private static TrackingServicesBootstrap _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
