using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Thesis.Core;

public class HandGestureRecognizer : MonoBehaviour
{
    [Header("UI（可选）")]
    public TMP_Text statusText;

    [Header("回调事件")]
    public UnityEvent OnOpenHand;
    public UnityEvent OnFist;
    public UnityEvent OnOK;
    public UnityEvent OnThumbsUp; // 保留兼容，但当前不使用

    [Header("阈值（可按实际调）")]
    [Range(0f, 1f)] public float okTipDist = 0.08f;
    [Range(0f, 1f)] public float fistAvgCurl = 0.18f;
    [Range(0f, 1f)] public float openAvgSpread = 0.22f;

    [Header("显示")]
    public bool showRawLabel = false;

    [Header("任务门控（Gate）")]
    public bool enableGate = true;
    public Gesture gateGesture = Gesture.Unknown;

    [Header("稳定性设置")]
    [Range(1, 20)] public int requiredStableFrames = 5;
    [Range(0f, 1f)] public float smoothFactor = 0.3f;

    [Header("Global State Bridge")]
    public bool pushToGlobalState = true;

    [Header("Debug")]
    public bool debugFeed = false;
    public bool debugPush = false;
    public bool debugClassify = false;

    Vector2[] _rawPoints;
    Vector2[] _smoothedPoints;

    Gesture _lastFrameGesture = Gesture.Unknown;
    int _stableFrameCount = 0;

    private string _lastPushedKey = "none";

    // 保留 ThumbsUp 仅为了兼容你现有 controller / overlay 代码
    public enum Gesture { Unknown, OK, ThumbsUp, Fist, Open }

    public Gesture CurrentGesture { get; private set; } = Gesture.Unknown;
    public Vector2[] LastPoints => _smoothedPoints ?? _rawPoints;

    // 给 controller 用的 confidence
    [Range(0f, 1f)] public float OpenConfidence { get; private set; } = 0f;
    [Range(0f, 1f)] public float FistConfidence { get; private set; } = 0f;
    [Range(0f, 1f)] public float OKConfidence { get; private set; } = 0f;
    [Range(0f, 1f)] public float ThumbsUpConfidence { get; private set; } = 0f; // 保留兼容，固定为 0

    void Awake()
    {
        if (statusText) statusText.text = "Ready...";
    }

    public void ApplyPreset(GesturePreset p)
    {
        okTipDist = p.okTipDist;
        fistAvgCurl = p.fistAvgCurl;
        openAvgSpread = p.openAvgSpread;
    }

    public void GateTo(string g)
    {
        gateGesture = FromString(g);
        enableGate = true;
    }

    public void ClearGate()
    {
        gateGesture = Gesture.Unknown;
        enableGate = false;
    }

    Gesture FromString(string s)
    {
        if (string.IsNullOrEmpty(s)) return Gesture.Unknown;
        switch (s.ToLowerInvariant())
        {
            case "ok":
                return Gesture.OK;

            // 保留解析，但当前不会真正识别出来
            case "thumbs_up":
            case "thumbsup":
            case "thumbs up":
                return Gesture.ThumbsUp;

            case "fist":
                return Gesture.Fist;

            case "open":
            case "open_hand":
            case "openhand":
            case "open hand":
                return Gesture.Open;

            default:
                return Gesture.Unknown;
        }
    }

    public void Feed(Vector2[] points)
    {
        _rawPoints = points;

        if (debugFeed)
            Debug.Log($"[HandGestureRecognizer] Feed points={(points == null ? 0 : points.Length)}", this);

        if (points == null || points.Length < 21)
        {
            CurrentGesture = Gesture.Unknown;
            OpenConfidence = 0f;
            FistConfidence = 0f;
            OKConfidence = 0f;
            ThumbsUpConfidence = 0f;

            _stableFrameCount = 0;
            _lastFrameGesture = Gesture.Unknown;

            PushGlobal("none");
            Write(showRawLabel ? "No hand" : null);
            return;
        }

        if (_smoothedPoints == null || _smoothedPoints.Length != points.Length)
        {
            _smoothedPoints = (Vector2[])points.Clone();
        }
        else
        {
            for (int i = 0; i < points.Length; i++)
                _smoothedPoints[i] = Vector2.Lerp(_smoothedPoints[i], points[i], smoothFactor);
        }

        ComputeConfidences(_smoothedPoints);
        var rawGesture = Classify(_smoothedPoints);

        if (enableGate && gateGesture != Gesture.Unknown && rawGesture != gateGesture)
            rawGesture = Gesture.Unknown;

        if (rawGesture == _lastFrameGesture)
        {
            _stableFrameCount++;
        }
        else
        {
            _stableFrameCount = 1;
            _lastFrameGesture = rawGesture;
        }

        Gesture stableGesture = (_stableFrameCount >= requiredStableFrames) ? rawGesture : Gesture.Unknown;
        CurrentGesture = stableGesture;

        switch (stableGesture)
        {
            case Gesture.OK:
                Write(showRawLabel ? "OK" : null);
                OnOK?.Invoke();
                PushGlobal("ok");
                break;

            case Gesture.Fist:
                Write(showRawLabel ? "Fist" : null);
                OnFist?.Invoke();
                PushGlobal("fist");
                break;

            case Gesture.Open:
                Write(showRawLabel ? "Open hand" : null);
                OnOpenHand?.Invoke();
                PushGlobal("open_hand");
                break;

            // 当前不会主动进入这里
            case Gesture.ThumbsUp:
                Write(showRawLabel ? "Thumbs Up" : null);
                OnThumbsUp?.Invoke();
                PushGlobal("thumbs_up");
                break;

            default:
                Write(showRawLabel ? "…" : null);
                PushGlobal("none");
                break;
        }
    }

    private void PushGlobal(string key)
    {
        if (!pushToGlobalState) return;
        if (_lastPushedKey == key) return;

        _lastPushedKey = key;

        var gs = ThesisGestureState.Instance;
        if (gs == null) return;

        if (debugPush)
            Debug.Log($"[HandGestureRecognizer] PushGlobal={key}", this);

        if (key == "none") gs.ClearGesture();
        else gs.SetGesture(key);
    }

    private void ComputeConfidences(Vector2[] lm)
    {
        const int WRIST = 0;
        const int THUMB_TIP = 4, INDEX_TIP = 8, MIDDLE_TIP = 12, RING_TIP = 16, PINKY_TIP = 20;
        const int INDEX_PIP = 6, MIDDLE_PIP = 10, RING_PIP = 14, PINKY_PIP = 18;
        const int INDEX_MCP = 5, MIDDLE_MCP = 9, RING_MCP = 13, PINKY_MCP = 17;

        float thumbIndex = Dist(lm[THUMB_TIP], lm[INDEX_TIP]);

        float indexCurl = Curl(lm[INDEX_TIP], lm[INDEX_PIP], lm[INDEX_MCP]);
        float middleCurl = Curl(lm[MIDDLE_TIP], lm[MIDDLE_PIP], lm[MIDDLE_MCP]);
        float ringCurl = Curl(lm[RING_TIP], lm[RING_PIP], lm[RING_MCP]);
        float pinkyCurl = Curl(lm[PINKY_TIP], lm[PINKY_PIP], lm[PINKY_MCP]);

        float avgCurl = (indexCurl + middleCurl + ringCurl + pinkyCurl) / 4f;

        float spread =
            (Dist(lm[INDEX_TIP], lm[WRIST]) +
             Dist(lm[MIDDLE_TIP], lm[WRIST]) +
             Dist(lm[RING_TIP], lm[WRIST]) +
             Dist(lm[PINKY_TIP], lm[WRIST])) / 4f;

        // Open confidence
        float openSpreadPart = Mathf.Clamp01(spread / Mathf.Max(openAvgSpread, 0.0001f));
        float openCurlPart = 1f - Mathf.Clamp01(avgCurl / 0.15f);
        OpenConfidence = Mathf.Clamp01(0.6f * openSpreadPart + 0.4f * openCurlPart);

        // Fist confidence
        FistConfidence = Mathf.Clamp01(avgCurl / Mathf.Max(fistAvgCurl, 0.0001f));

        // OK confidence
        float okTouchPart = 1f - Mathf.Clamp01(thumbIndex / Mathf.Max(okTipDist, 0.0001f));
        float okCurlPart = 1f - Mathf.Clamp01(Mathf.Abs(avgCurl - 0.12f) / 0.18f);
        OKConfidence = Mathf.Clamp01(0.75f * okTouchPart + 0.25f * okCurlPart);

        // 取消 thumbs up 识别：保持兼容，但固定为 0
        ThumbsUpConfidence = 0f;

        if (debugClassify)
        {
            Debug.Log(
                $"[HandGestureRecognizer] OpenConf={OpenConfidence:0.00}, FistConf={FistConfidence:0.00}, OKConf={OKConfidence:0.00}, ThumbsUpConf={ThumbsUpConfidence:0.00} | " +
                $"avgCurl={avgCurl:0.000}, spread={spread:0.000}, thumbIndex={thumbIndex:0.000}",
                this
            );
        }
    }

    Gesture Classify(Vector2[] lm)
    {
        const int WRIST = 0;
        const int THUMB_TIP = 4, INDEX_TIP = 8, MIDDLE_TIP = 12, RING_TIP = 16, PINKY_TIP = 20;
        const int INDEX_PIP = 6, MIDDLE_PIP = 10, RING_PIP = 14, PINKY_PIP = 18;
        const int INDEX_MCP = 5, MIDDLE_MCP = 9, RING_MCP = 13, PINKY_MCP = 17;

        float thumbIndex = Dist(lm[THUMB_TIP], lm[INDEX_TIP]);

        float indexCurl = Curl(lm[INDEX_TIP], lm[INDEX_PIP], lm[INDEX_MCP]);
        float middleCurl = Curl(lm[MIDDLE_TIP], lm[MIDDLE_PIP], lm[MIDDLE_MCP]);
        float ringCurl = Curl(lm[RING_TIP], lm[RING_PIP], lm[RING_MCP]);
        float pinkyCurl = Curl(lm[PINKY_TIP], lm[PINKY_PIP], lm[PINKY_MCP]);
        float avgCurl = (indexCurl + middleCurl + ringCurl + pinkyCurl) / 4f;

        float spread =
            (Dist(lm[INDEX_TIP], lm[WRIST]) +
             Dist(lm[MIDDLE_TIP], lm[WRIST]) +
             Dist(lm[RING_TIP], lm[WRIST]) +
             Dist(lm[PINKY_TIP], lm[WRIST])) / 4f;

        if (thumbIndex < okTipDist && avgCurl < 0.12f) return Gesture.OK;
        if (avgCurl > fistAvgCurl) return Gesture.Fist;
        if (spread > openAvgSpread && avgCurl < 0.15f) return Gesture.Open;

        return Gesture.Unknown;
    }

    float Dist(Vector2 a, Vector2 b) => (a - b).magnitude;

    float Curl(Vector2 tip, Vector2 pip, Vector2 mcp)
    {
        float a = Dist(tip, mcp);
        float b = Dist(pip, mcp) + 1e-5f;
        return 1f - Mathf.Clamp01(a / b);
    }

    void Write(string s)
    {
        if (!statusText) return;
        if (string.IsNullOrEmpty(s)) return;
        statusText.text = s;
    }
}