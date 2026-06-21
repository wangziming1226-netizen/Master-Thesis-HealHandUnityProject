using UnityEngine;

namespace Thesis.Core
{
    public class ThesisServicesBootstrap : MonoBehaviour
    {
        [Header("Prefab")]
        [Tooltip("Drag your ThesisServices prefab here.")]
        public GameObject thesisServicesPrefab;

        private static GameObject _instance;

        private void Awake()
        {
            if (_instance != null) return;

            if (thesisServicesPrefab == null)
            {
                Debug.LogError("[ServicesBootstrap] thesisServicesPrefab is NULL. Please assign the ThesisServices prefab.");
                return;
            }

            _instance = Instantiate(thesisServicesPrefab);
            _instance.name = thesisServicesPrefab.name; // keep clean name
            DontDestroyOnLoad(_instance);

            Debug.Log("[ServicesBootstrap] ThesisServices instantiated & marked DontDestroyOnLoad.");
        }
    }
}