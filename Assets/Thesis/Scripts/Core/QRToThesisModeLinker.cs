using System;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class QRToThesisModeLinker : MonoBehaviour
{
    [Serializable]
    public class DifficultyPreset
    {
        public int level = 1;
        public float targetHoldSeconds = 3.0f;
        public float maxTaskSeconds = 10.0f;
        public float strongMatchThreshold = 0.80f;
        public float weakMatchThreshold = 0.45f;
        public float strongMatchMargin = 0.12f;
        public float weakMatchProgressMultiplier = 0.35f;
    }

    [Serializable]
    public class CardAppliedEvent : UnityEvent<string, int, bool> { }

    [Serializable]
    private class QRTaskConfigJson
    {
        public string card_id;
        public string gesture;
        public string difficulty; // Supports older cards that use easy/medium/hard or "1"/"2"/... as difficulty values.
        public float hold_secs;
    }

    [Header("Scanner")]
    public QRScanner scanner;
    public TMP_Text guide;

    [Header("Target Controllers")]
    [Tooltip("When assigned, scanned task settings are applied to this controller first.")]
    public MonoBehaviour explicitTargetController;

    [Tooltip("Optional: when explicitTargetController is empty, choose from the three controllers below based on which one is active.")]
    public MonoBehaviour practiceController;
    public MonoBehaviour assessmentController;
    public MonoBehaviour dailyController;

    [Header("Mode")]
    [Tooltip("false = Fixed; true = Adaptive, using the scanned difficulty as the starting point.")]
    public bool adaptiveEnabled = false;

    [Tooltip("Automatically attempt to start the task after a QR code is successfully scanned.")]
    public bool autoStartAfterApply = true;

    [Tooltip("Restart scanning immediately after a successful scan. This is normally left disabled.")]
    public bool restartScannerImmediately = false;

    [Header("Difficulty Presets")]
    public DifficultyPreset level1 = new DifficultyPreset
    {
        level = 1,
        targetHoldSeconds = 2.0f,
        maxTaskSeconds = 8.0f,
        strongMatchThreshold = 0.72f,
        weakMatchThreshold = 0.38f,
        strongMatchMargin = 0.08f,
        weakMatchProgressMultiplier = 0.50f
    };

    public DifficultyPreset level2 = new DifficultyPreset
    {
        level = 2,
        targetHoldSeconds = 2.5f,
        maxTaskSeconds = 9.0f,
        strongMatchThreshold = 0.78f,
        weakMatchThreshold = 0.42f,
        strongMatchMargin = 0.10f,
        weakMatchProgressMultiplier = 0.42f
    };

    public DifficultyPreset level3 = new DifficultyPreset
    {
        level = 3,
        targetHoldSeconds = 3.0f,
        maxTaskSeconds = 10.0f,
        strongMatchThreshold = 0.80f,
        weakMatchThreshold = 0.45f,
        strongMatchMargin = 0.12f,
        weakMatchProgressMultiplier = 0.35f
    };

    public DifficultyPreset level4 = new DifficultyPreset
    {
        level = 4,
        targetHoldSeconds = 3.5f,
        maxTaskSeconds = 11.0f,
        strongMatchThreshold = 0.84f,
        weakMatchThreshold = 0.50f,
        strongMatchMargin = 0.15f,
        weakMatchProgressMultiplier = 0.25f
    };

    [Header("Runtime Debug")]
    public string lastCardId = "";
    public string lastGesture = "";
    public int lastDifficultyLevel = 1;
    public float lastHoldSecs = 0f;
    public string lastGestureEnumName = "";
    public string lastTargetControllerName = "";
    public string lastStatus = "Idle";

    [Header("Events")]
    [Tooltip("Parameters: gesture, difficultyLevel, adaptiveEnabled")]
    public CardAppliedEvent onCardApplied;

    void Awake()
    {
        if (!guide)
            guide = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
    }

    void OnEnable()
    {
        if (!scanner)
            scanner = FindFirstObjectByType<QRScanner>(FindObjectsInactive.Include);

        if (scanner)
            scanner.onDecoded.AddListener(OnDecoded);
    }

    void OnDisable()
    {
        if (scanner)
            scanner.onDecoded.RemoveListener(OnDecoded);
    }

    public void OnDecoded(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            lastStatus = "Decoded payload empty";
            SetGuide("Scanned empty payload.");
            return;
        }

        string cardId;
        string gestureToken;
        string difficultyToken;
        float holdSecs;

        if (!TryParseCardConfig(payload, out cardId, out gestureToken, out difficultyToken, out holdSecs))
        {
            lastStatus = "Invalid card payload";
            SetGuide("Scanned non-card content.");
            return;
        }

        int difficultyLevel = ParseDifficultyLevel(difficultyToken);
        DifficultyPreset preset = GetPreset(difficultyLevel);

        string gestureEnumName = MapGestureToEnumName(gestureToken);
        if (string.IsNullOrEmpty(gestureEnumName))
        {
            lastStatus = $"Unsupported gesture: {gestureToken}";
            SetGuide($"Unsupported gesture: {gestureToken}");
            return;
        }

        MonoBehaviour target = ResolveTargetController();
        if (!target)
        {
            lastStatus = "No target controller";
            SetGuide("No active mode controller found.");
            return;
        }

        lastCardId = cardId ?? "";
        lastGesture = gestureToken ?? "";
        lastDifficultyLevel = difficultyLevel;
        lastHoldSecs = holdSecs > 0f ? holdSecs : preset.targetHoldSeconds;
        lastGestureEnumName = gestureEnumName;
        lastTargetControllerName = target.name;

        try
        {
            ApplyToController(target, gestureEnumName, difficultyLevel, holdSecs, preset, adaptiveEnabled);

            lastStatus = "Applied";
            SetGuide(
                $"Card {lastCardId}  •  {gestureEnumName}  •  Level {difficultyLevel}  •  " +
                $"{(adaptiveEnabled ? "Adaptive start" : "Fixed")}"
            );

            onCardApplied?.Invoke(lastGesture, lastDifficultyLevel, adaptiveEnabled);

            if (autoStartAfterApply)
                TryInvokeMethod(target, "OnStart");

            if (restartScannerImmediately && scanner)
                scanner.RestartScan();
        }
        catch (Exception ex)
        {
            lastStatus = "Apply failed";
            Debug.LogError($"[QRToThesisModeLinker] Apply failed: {ex.Message}");
            SetGuide("Apply failed. Check console.");
        }
    }

    private MonoBehaviour ResolveTargetController()
    {
        if (explicitTargetController != null)
            return explicitTargetController;

        if (practiceController != null && practiceController.gameObject.activeInHierarchy)
            return practiceController;

        if (assessmentController != null && assessmentController.gameObject.activeInHierarchy)
            return assessmentController;

        if (dailyController != null && dailyController.gameObject.activeInHierarchy)
            return dailyController;

        return null;
    }

    private void ApplyToController(
        MonoBehaviour controller,
        string gestureEnumName,
        int difficultyLevel,
        float holdSecsFromCard,
        DifficultyPreset preset,
        bool adaptive)
    {
        float finalHoldSecs = holdSecsFromCard > 0f ? holdSecsFromCard : preset.targetHoldSeconds;

        // 1) Gesture enum
        FieldInfo targetGestureField = GetFieldRecursive(controller.GetType(), "targetGestureType");
        if (targetGestureField != null)
        {
            object enumValue = Enum.Parse(targetGestureField.FieldType, gestureEnumName);
            targetGestureField.SetValue(controller, enumValue);
        }

        // 2) Difficulty preset -> common thesis controller fields
        SetFieldIfExists(controller, "targetHoldSeconds", finalHoldSecs);
        SetFieldIfExists(controller, "maxTaskSeconds", preset.maxTaskSeconds);
        SetFieldIfExists(controller, "strongMatchThreshold", preset.strongMatchThreshold);
        SetFieldIfExists(controller, "weakMatchThreshold", preset.weakMatchThreshold);
        SetFieldIfExists(controller, "strongMatchMargin", preset.strongMatchMargin);
        SetFieldIfExists(controller, "weakMatchProgressMultiplier", preset.weakMatchProgressMultiplier);

        // 3) Optional metadata / adaptive hints
        ApplyAdaptiveHints(controller, adaptive, difficultyLevel, gestureEnumName);

        // 4) Reset / refresh UI
        // First try the controller’s existing OnReset method.
        bool resetCalled = TryInvokeMethod(controller, "OnReset");

        if (!resetCalled)
        {
            TryInvokeMethod(controller, "InitTexts");
            TryInvokeMethod(controller, "RecalculateScore");
            TryInvokeRefreshUnifiedOutput(controller);
            TryInvokeRefreshUI(controller);
        }
    }

    private void ApplyAdaptiveHints(MonoBehaviour controller, bool adaptive, int difficultyLevel, string gestureEnumName)
    {
        // Common Boolean mode flags.
        SetFieldIfExists(controller, "adaptiveEnabled", adaptive);
        SetFieldIfExists(controller, "useAdaptiveMode", adaptive);
        SetFieldIfExists(controller, "isAdaptiveMode", adaptive);

        // Common difficulty metadata.
        SetFieldIfExists(controller, "baseDifficultyLevel", difficultyLevel);
        SetFieldIfExists(controller, "currentDifficultyLevel", difficultyLevel);
        SetFieldIfExists(controller, "initialDifficultyLevel", difficultyLevel);
        SetFieldIfExists(controller, "startDifficultyLevel", difficultyLevel);
        SetFieldIfExists(controller, "scannedDifficultyLevel", difficultyLevel);

        // Common card metadata.
        SetFieldIfExists(controller, "lastScannedCardId", lastCardId);
        SetFieldIfExists(controller, "lastScannedGesture", gestureEnumName);
        SetFieldIfExists(controller, "lastScannedDifficulty", difficultyLevel);

        // Common mode setter methods.
        TryInvokeMethod(controller, "SetAdaptiveMode", new object[] { adaptive });
    }

    private DifficultyPreset GetPreset(int level)
    {
        switch (level)
        {
            case 1: return level1;
            case 2: return level2;
            case 3: return level3;
            case 4: return level4;
            default: return level1;
        }
    }

    private int ParseDifficultyLevel(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return 1;

        string t = token.Trim().ToLowerInvariant();

        switch (t)
        {
            case "easy":
                return 1;
            case "medium":
                return 2;
            case "hard":
                return 4;
        }

        if (int.TryParse(t, out int level))
            return Mathf.Clamp(level, 1, 4);

        return 1;
    }

    private string MapGestureToEnumName(string gestureToken)
    {
        if (string.IsNullOrWhiteSpace(gestureToken))
            return null;

        string g = gestureToken.Trim().ToLowerInvariant();

        switch (g)
        {
            case "open":
            case "open_hand":
                return "OpenHand";
            case "fist":
                return "Fist";
            case "ok":
                return "OK";
            default:
                return null;
        }
    }

    private bool TryParseCardConfig(
        string payload,
        out string cardId,
        out string gesture,
        out string difficulty,
        out float holdSecs)
    {
        cardId = "";
        gesture = "";
        difficulty = "";
        holdSecs = 0f;

        // First try the legacy JsonUtility format.
        try
        {
            var cfg = JsonUtility.FromJson<QRTaskConfigJson>(payload);
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.gesture))
            {
                cardId = cfg.card_id ?? "";
                gesture = cfg.gesture ?? "";
                difficulty = cfg.difficulty ?? "";
                holdSecs = cfg.hold_secs;
                return true;
            }
        }
        catch
        {
            // ignore
        }

        // Then use a regex fallback to support formats such as "difficulty":2.
        gesture = ExtractJsonString(payload, "gesture");
        if (string.IsNullOrWhiteSpace(gesture))
            return false;

        cardId = ExtractJsonString(payload, "card_id");
        difficulty = ExtractJsonStringOrNumber(payload, "difficulty");
        holdSecs = ExtractJsonFloat(payload, "hold_secs", 0f);

        return true;
    }

    private string ExtractJsonString(string json, string key)
    {
        string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*\"([^\"]*)\"";
        Match m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "";
    }

    private string ExtractJsonStringOrNumber(string json, string key)
    {
        string asString = ExtractJsonString(json, key);
        if (!string.IsNullOrEmpty(asString))
            return asString;

        string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*(-?\\d+)";
        Match m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "";
    }

    private float ExtractJsonFloat(string json, string key, float defaultValue)
    {
        string pattern = $"\"{Regex.Escape(key)}\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)";
        Match m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        if (m.Success && float.TryParse(m.Groups[1].Value, out float v))
            return v;
        return defaultValue;
    }

    private void SetGuide(string message)
    {
        if (guide) guide.text = message;
        Debug.Log($"[QRToThesisModeLinker] {message}");
    }

    private void SetFieldIfExists(object obj, string fieldName, object value)
    {
        if (obj == null) return;

        FieldInfo f = GetFieldRecursive(obj.GetType(), fieldName);
        if (f == null) return;

        object converted = ConvertValue(value, f.FieldType);
        f.SetValue(obj, converted);
    }

    private FieldInfo GetFieldRecursive(Type t, string fieldName)
    {
        while (t != null)
        {
            FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f;
            t = t.BaseType;
        }
        return null;
    }

    private object ConvertValue(object value, Type targetType)
    {
        if (value == null) return null;

        Type nonNullable = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (nonNullable.IsAssignableFrom(value.GetType()))
            return value;

        if (nonNullable.IsEnum && value is string s)
            return Enum.Parse(nonNullable, s);

        return Convert.ChangeType(value, nonNullable);
    }

    private bool TryInvokeMethod(object obj, string methodName)
    {
        return TryInvokeMethod(obj, methodName, null);
    }

    private bool TryInvokeMethod(object obj, string methodName, object[] args)
    {
        if (obj == null) return false;

        Type t = obj.GetType();
        MethodInfo found = null;

        while (t != null && found == null)
        {
            MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                if (m.Name != methodName) continue;

                var ps = m.GetParameters();
                int argCount = args == null ? 0 : args.Length;
                if (ps.Length != argCount) continue;

                found = m;
                break;
            }
            t = t.BaseType;
        }

        if (found == null)
            return false;

        found.Invoke(obj, args);
        return true;
    }

    private void TryInvokeRefreshUnifiedOutput(object obj)
    {
        if (obj == null) return;

        Type t = obj.GetType();
        MethodInfo m = null;

        while (t != null && m == null)
        {
            m = t.GetMethod("RefreshUnifiedOutput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            t = t.BaseType;
        }

        if (m == null) return;

        var ps = m.GetParameters();
        if (ps.Length == 2 && ps[0].ParameterType == typeof(bool) && ps[1].ParameterType == typeof(string))
        {
            m.Invoke(obj, new object[] { false, "none" });
        }
    }

    private void TryInvokeRefreshUI(object obj)
    {
        if (obj == null) return;

        Type t = obj.GetType();
        MethodInfo m = null;

        while (t != null && m == null)
        {
            m = t.GetMethod("RefreshUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            t = t.BaseType;
        }

        if (m == null) return;

        var ps = m.GetParameters();
        if (ps.Length == 1 && ps[0].ParameterType == typeof(bool))
        {
            m.Invoke(obj, new object[] { false });
        }
    }
}