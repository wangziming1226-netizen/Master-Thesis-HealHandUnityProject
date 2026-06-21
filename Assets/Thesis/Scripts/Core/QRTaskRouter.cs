
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class QRTaskRouter : MonoBehaviour
{
    [Serializable]
    public class QRTaskConfig
    {
        public string card_id;
        public string gesture;
        public int difficulty;
        public float hold_secs;
    }

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

    [Header("Scanner")]
    public QRScanner scanner;
    public TMP_Text guide;

    [Header("Auto Find")]
    public bool autoFindScanner = true;
    public bool autoFindControllers = true;
    public float refindInterval = 1f;

    [Header("Optional Manual Target")]
    public MonoBehaviour targetController;

    [Header("Optional Cached Controllers")]
    public MonoBehaviour practiceController;
    public MonoBehaviour assessmentController;
    public MonoBehaviour dailyController;

    [Header("Mode Routing")]
    public bool overrideModeOnScan = false;
    public bool adaptiveEnabled = false;

    [Tooltip("Automatically attempt to start the task after a QR code is successfully scanned.")]
    public bool autoStartAfterApply = true;

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
    public int lastDifficulty = 1;
    public float lastHoldSecs = 0f;
    public string lastAppliedGestureEnumName = "";
    public string lastTargetControllerName = "";
    public bool lastAppliedAdaptive = false;
    public string lastStatus = "Idle";

    [Header("Events")]
    public CardAppliedEvent onCardApplied;

    private float nextRefindTime = 0f;
    private bool scannerHooked = false;

    void Awake()
    {
        // Keep this null by default unless explicitly wired.
        // Avoid auto-grabbing an unrelated TMP_Text such as mode/difficulty labels.
    }

    void OnEnable()
    {
        TryResolveReferences();
        TryHookScanner();
    }

    void OnDisable()
    {
        UnhookScanner();
    }

    void Update()
    {
        if (Time.time < nextRefindTime) return;
        nextRefindTime = Time.time + refindInterval;

        TryResolveReferences();
        TryHookScanner();
    }

    private void TryResolveReferences()
    {
        if (autoFindScanner && scanner == null)
        {
#if UNITY_2023_1_OR_NEWER
            scanner = FindFirstObjectByType<QRScanner>(FindObjectsInactive.Include);
#else
            scanner = FindObjectOfType<QRScanner>(true);
#endif
        }

        if (!autoFindControllers) return;

        if (practiceController == null)
            practiceController = FindControllerByTypeName("PracticeModeController");

        if (assessmentController == null)
            assessmentController = FindControllerByTypeName("AssessmentModeController");

        if (dailyController == null)
            dailyController = FindControllerByTypeName("DailyModeController");
    }

    private void TryHookScanner()
    {
        if (scanner == null) return;
        if (scannerHooked) return;

        scanner.onDecoded.RemoveListener(OnDecodedJson);
        scanner.onDecoded.AddListener(OnDecodedJson);
        scannerHooked = true;

        Debug.Log($"[QRTaskRouter:{name}#{GetInstanceID()}] Scanner hooked.", this);
    }

    private void UnhookScanner()
    {
        if (scanner != null && scannerHooked)
            scanner.onDecoded.RemoveListener(OnDecodedJson);

        scannerHooked = false;
    }

    private MonoBehaviour FindControllerByTypeName(string typeName)
    {
#if UNITY_2023_1_OR_NEWER
        var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var all = FindObjectsOfType<MonoBehaviour>(true);
#endif
        foreach (var mb in all)
        {
            if (mb == null) continue;
            if (mb.GetType().Name == typeName)
                return mb;
        }
        return null;
    }

    public void OnDecodedJson(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            lastStatus = "Decoded text empty";
            SetGuide("Scanned empty payload.");
            return;
        }

        QRTaskConfig cfg = null;
        try
        {
            cfg = JsonUtility.FromJson<QRTaskConfig>(rawText);
        }
        catch (Exception ex)
        {
            lastStatus = "JSON parse failed";
            Debug.LogError($"[QRTaskRouter] JSON parse failed: {ex.Message}\nRaw={rawText}");
            return;
        }

        if (cfg == null || string.IsNullOrWhiteSpace(cfg.gesture))
        {
            lastStatus = "Config invalid";
            SetGuide("Scanned non-card content.");
            return;
        }

        int difficulty = Mathf.Clamp(cfg.difficulty <= 0 ? 1 : cfg.difficulty, 1, 4);
        DifficultyPreset preset = GetPreset(difficulty);

        string enumName = MapGestureToEnumName(cfg.gesture);
        if (string.IsNullOrEmpty(enumName))
        {
            lastStatus = $"Unsupported gesture: {cfg.gesture}";
            SetGuide($"Unsupported gesture: {cfg.gesture}");
            return;
        }

        MonoBehaviour controller = ResolveTargetController();
        if (controller == null)
        {
            lastStatus = "Target controller missing";
            SetGuide("No active mode controller found.");
            Debug.LogWarning("[QRTaskRouter] No active target controller found.");
            return;
        }

        bool effectiveAdaptive = ResolveAdaptiveModeForScan(controller);

        lastCardId = cfg.card_id ?? "";
        lastGesture = cfg.gesture;
        lastDifficulty = difficulty;
        lastHoldSecs = cfg.hold_secs;
        lastAppliedGestureEnumName = enumName;
        lastAppliedAdaptive = effectiveAdaptive;
        lastTargetControllerName = controller.name;

        try
        {
            ApplyConfigToController(controller, cfg, preset, enumName, effectiveAdaptive);

            lastStatus = "Applied";
            SetGuide($"Card {lastCardId} • {enumName} • Level {difficulty} • {(effectiveAdaptive ? "Adaptive" : "Fixed")}");

            onCardApplied?.Invoke(cfg.gesture, difficulty, effectiveAdaptive);

            if (autoStartAfterApply)
                TryInvokeMethod(controller, "OnStart");
        }
        catch (Exception ex)
        {
            lastStatus = "Apply failed";
            Debug.LogError($"[QRTaskRouter] Apply failed: {ex}");
            SetGuide("Apply failed. Check console.");
        }
    }

    private MonoBehaviour ResolveTargetController()
    {
        if (targetController != null)
            return targetController;

        if (practiceController != null && practiceController.gameObject.activeInHierarchy)
            return practiceController;

        if (assessmentController != null && assessmentController.gameObject.activeInHierarchy)
            return assessmentController;

        if (dailyController != null && dailyController.gameObject.activeInHierarchy)
            return dailyController;

        if (autoFindControllers)
        {
            TryResolveReferences();

            if (practiceController != null && practiceController.gameObject.activeInHierarchy)
                return practiceController;
            if (assessmentController != null && assessmentController.gameObject.activeInHierarchy)
                return assessmentController;
            if (dailyController != null && dailyController.gameObject.activeInHierarchy)
                return dailyController;
        }

        return null;
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

    private string MapGestureToEnumName(string gesture)
    {
        string g = gesture.Trim().ToLowerInvariant();
        switch (g)
        {
            case "open_hand":
            case "open":
                return "OpenHand";
            case "fist":
                return "Fist";
            case "ok":
                return "OK";
            default:
                return null;
        }
    }

    private bool ResolveAdaptiveModeForScan(MonoBehaviour controller)
    {
        if (overrideModeOnScan)
            return adaptiveEnabled;

        bool currentMode;
        if (TryGetCurrentAdaptiveMode(controller, out currentMode))
            return currentMode;

        return adaptiveEnabled;
    }

    private bool TryGetCurrentAdaptiveMode(object obj, out bool value)
    {
        value = false;
        if (obj == null) return false;

        if (TryReadBoolMember(obj, "adaptiveEnabled", out value)) return true;
        if (TryReadBoolMember(obj, "useAdaptiveMode", out value)) return true;
        if (TryReadBoolMember(obj, "isAdaptiveMode", out value)) return true;

        FieldInfo adaptiveControllerField = GetFieldRecursive(obj.GetType(), "adaptiveController");
        if (adaptiveControllerField != null)
        {
            object adaptiveControllerObj = adaptiveControllerField.GetValue(obj);
            if (adaptiveControllerObj != null)
            {
                if (TryReadBoolMember(adaptiveControllerObj, "adaptiveEnabled", out value)) return true;
                if (TryReadBoolMember(adaptiveControllerObj, "useAdaptiveMode", out value)) return true;
                if (TryReadBoolMember(adaptiveControllerObj, "isAdaptiveMode", out value)) return true;
            }
        }

        return false;
    }

    private bool TryReadBoolMember(object obj, string memberName, out bool value)
    {
        value = false;
        if (obj == null) return false;

        FieldInfo f = GetFieldRecursive(obj.GetType(), memberName);
        if (f != null && (f.FieldType == typeof(bool) || f.FieldType == typeof(bool?)))
        {
            object raw = f.GetValue(obj);
            if (raw != null)
            {
                value = (bool)raw;
                return true;
            }
        }

        PropertyInfo p = GetPropertyRecursive(obj.GetType(), memberName);
        if (p != null && p.CanRead && (p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?)))
        {
            object raw = p.GetValue(obj);
            if (raw != null)
            {
                value = (bool)raw;
                return true;
            }
        }

        return false;
    }

    private void ApplyConfigToController(
        MonoBehaviour controller,
        QRTaskConfig cfg,
        DifficultyPreset preset,
        string enumName,
        bool effectiveAdaptive)
    {
        float holdSecs = cfg.hold_secs > 0f ? cfg.hold_secs : preset.targetHoldSeconds;

        // Practice has a dedicated adaptive controller; avoid reseeding it on every scan.
        if (controller.GetType().Name == "PracticeModeController")
        {
            bool appliedBySpecialMethod = TryInvokeMethod(
                controller,
                "ApplyScannedTask",
                new object[]
                {
                    lastCardId,
                    lastDifficulty,
                    enumName,
                    effectiveAdaptive,
                    holdSecs,
                    preset.maxTaskSeconds,
                    preset.strongMatchThreshold,
                    preset.weakMatchThreshold,
                    preset.strongMatchMargin,
                    preset.weakMatchProgressMultiplier
                }
            );

            if (appliedBySpecialMethod)
                return;
        }

        Type t = controller.GetType();

        FieldInfo targetGestureField = GetFieldRecursive(t, "targetGestureType");
        if (targetGestureField != null)
        {
            object enumValue = Enum.Parse(targetGestureField.FieldType, enumName);
            targetGestureField.SetValue(controller, enumValue);
        }

        SetFieldIfExists(controller, "targetHoldSeconds", holdSecs);
        SetFieldIfExists(controller, "maxTaskSeconds", preset.maxTaskSeconds);
        SetFieldIfExists(controller, "strongMatchThreshold", preset.strongMatchThreshold);
        SetFieldIfExists(controller, "weakMatchThreshold", preset.weakMatchThreshold);
        SetFieldIfExists(controller, "strongMatchMargin", preset.strongMatchMargin);
        SetFieldIfExists(controller, "weakMatchProgressMultiplier", preset.weakMatchProgressMultiplier);

        SetFieldIfExists(controller, "sessionId", BuildSessionId(cfg, effectiveAdaptive));
        SetFieldIfExists(controller, "adaptiveEnabled", effectiveAdaptive);
        SetFieldIfExists(controller, "useAdaptiveMode", effectiveAdaptive);
        SetFieldIfExists(controller, "isAdaptiveMode", effectiveAdaptive);

        SetFieldIfExists(controller, "baseDifficultyLevel", lastDifficulty);
        SetFieldIfExists(controller, "currentDifficultyLevel", lastDifficulty);
        SetFieldIfExists(controller, "initialDifficultyLevel", lastDifficulty);
        SetFieldIfExists(controller, "startDifficultyLevel", lastDifficulty);
        SetFieldIfExists(controller, "scannedDifficultyLevel", lastDifficulty);

        SetFieldIfExists(controller, "lastScannedCardId", lastCardId);
        SetFieldIfExists(controller, "lastScannedGesture", enumName);
        SetFieldIfExists(controller, "lastScannedDifficulty", lastDifficulty);

        TryInvokeMethod(controller, "SetAdaptiveMode", new object[] { effectiveAdaptive });

        bool calledReset = TryInvokeMethod(controller, "OnReset");
        TryInvokeMethod(controller, "SetScannedCardContext", new object[] { lastCardId, lastDifficulty });

        if (!calledReset)
        {
            TryInvokeMethod(controller, "InitTexts");
            TryInvokeMethod(controller, "RecalculateScore");
            TryInvokeRefreshUnifiedOutput(controller);
            TryInvokeRefreshUI(controller);
        }
        else
        {
            TryInvokeRefreshUnifiedOutput(controller);
            TryInvokeRefreshUI(controller);
        }
    }

    private string BuildSessionId(QRTaskConfig cfg, bool effectiveAdaptive)
    {
        string card = string.IsNullOrWhiteSpace(cfg.card_id) ? "card" : cfg.card_id.Trim();
        string g = string.IsNullOrWhiteSpace(cfg.gesture) ? "gesture" : cfg.gesture.Trim();
        string mode = effectiveAdaptive ? "adaptive" : "fixed";
        return $"{card}_{g}_{lastDifficulty}_{mode}";
    }

    private void SetGuide(string message)
    {
        if (guide != null)
            guide.text = message;

        Debug.Log($"[QRTaskRouter:{name}#{GetInstanceID()} scene={gameObject.scene.name}] {message}", this);
    }

    private void SetFieldIfExists(object obj, string fieldName, object value)
    {
        if (obj == null) return;

        FieldInfo f = GetFieldRecursive(obj.GetType(), fieldName);
        if (f == null) return;

        object converted = ConvertValue(value, f.FieldType);
        f.SetValue(obj, converted);
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

    private PropertyInfo GetPropertyRecursive(Type t, string propertyName)
    {
        while (t != null)
        {
            PropertyInfo p = t.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null) return p;
            t = t.BaseType;
        }
        return null;
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
