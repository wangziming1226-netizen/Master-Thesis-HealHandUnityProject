using UnityEngine;

namespace Thesis.Modules.Practice
{
    public static class PracticeConfigLoader
    {
        public static PracticeTaskConfigFile LoadFromResources(string resourcePathNoExt)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePathNoExt);
            if (jsonAsset == null)
            {
                Debug.LogError($"[Config] Not found at Resources/{resourcePathNoExt}.json");
                return null;
            }

            var cfg = JsonUtility.FromJson<PracticeTaskConfigFile>(jsonAsset.text);
            if (cfg == null || cfg.tasks == null || cfg.tasks.Count == 0)
            {
                Debug.LogError($"[Config] Loaded but empty/invalid: Resources/{resourcePathNoExt}.json");
                return null;
            }

            Debug.Log($"[Config] Loaded version={cfg.version}, tasks={cfg.tasks.Count} from {resourcePathNoExt}");
            return cfg;
        }
    }
}
