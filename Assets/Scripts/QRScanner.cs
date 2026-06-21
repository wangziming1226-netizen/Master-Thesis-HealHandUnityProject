using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using ZXing;
using ZXing.Common;

#if UNITY_ENGINE
#endif

[DisallowMultipleComponent]
public class QRScanner : MonoBehaviour
{
    [Header("UI")]
    public RawImage preview;
    public AspectRatioFitter fitter;
    public TMP_Text statusText;

    [Header("Scan")]
    [Range(0.1f, 1f)] public float scanInterval = 0.3f;
    public bool openUrlIfLink = true;

    [Tooltip("Keep this as a backup. The scanner can still restart when the user taps/clicks.")]
    public bool autoRestartOnTap = true;

    [Tooltip("Automatically restart scanning after a valid QR code is decoded.")]
    public bool autoRestartAfterDecode = true;

    [Tooltip("Delay before the scanner becomes ready for the next QR code.")]
    public float autoRestartDelay = 0.8f;

    [Tooltip("If true, the same QR payload will not be accepted again until the card disappears from camera view.")]
    public bool requireClearBeforeSameCode = true;

    public AudioClip beep;

    [Header("Events")]
    public UnityEvent<string> onDecoded;

    [Header("Camera Source (optional)")]
    [Tooltip("如果有 CameraTextureTap 就拖；没有就会退回用 preview.texture")]
    public CameraTextureTap cameraTap;

    BarcodeReaderGeneric reader;
    bool scanning = true;
    float nextScan;

    Texture2D scratchTex;
    byte[] rgbaBuffer;

    // New state variables to prevent duplicate scans of the same visible card
    string lastAcceptedPayload = "";
    bool waitingForCardToClear = false;
    Coroutine restartCoroutine;

    [Serializable]
    public class CardConfig
    {
        public string card_id;
        public string gesture;
        public string difficulty;
        public float hold_secs;
    }

    void Start()
    {
        Application.targetFrameRate = 60;

        if (!cameraTap)
            cameraTap = FindFirstObjectByType<CameraTextureTap>(FindObjectsInactive.Include);

        reader = new BarcodeReaderGeneric
        {
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        if (fitter == null && preview != null)
            fitter = preview.GetComponent<AspectRatioFitter>();

        if (statusText) statusText.text = "Scan a QR card to start";
    }

    void Update()
    {
        Texture src = null;

        // 1) 优先走 CameraTextureTap（兼容旧 card 环境）
        if (!cameraTap)
            cameraTap = FindFirstObjectByType<CameraTextureTap>(FindObjectsInactive.Include);

        if (cameraTap != null)
            src = cameraTap.CurrentTexture;

        // 2) 没有 CameraTextureTap 就退回用 preview.texture（兼容 thesis 透明承载层）
        if (src == null && preview != null)
            src = preview.texture;

        if (src == null)
        {
            if (statusText) statusText.text = "Waiting for camera…";
            return;
        }

        // UI 比例
        if (fitter)
        {
            float w = src.width;
            float h = src.height;
            if (w > 0 && h > 0)
                fitter.aspectRatio = w / h;
        }

        // Keep tap restart as backup
        if (autoRestartOnTap && Input.GetMouseButtonDown(0))
            RestartScan();

        if (scanning && Time.time >= nextScan)
        {
            nextScan = Time.time + scanInterval;
            TryDecodeFromTexture(src);
        }
    }

    void TryDecodeFromTexture(Texture src)
    {
        if (!src) return;

        int w = src.width;
        int h = src.height;
        if (w <= 0 || h <= 0) return;

        Texture2D cpuTex = EnsureScratchTexture(w, h);

        if (src is Texture2D tex2D)
        {
            var pixels = tex2D.GetPixels32();
            cpuTex.SetPixels32(pixels);
            cpuTex.Apply(false);
        }
        else if (src is RenderTexture rt)
        {
            RenderTexture current = RenderTexture.active;
            RenderTexture.active = rt;
            cpuTex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            cpuTex.Apply(false);
            RenderTexture.active = current;
        }
        else if (src is WebCamTexture wct)
        {
            var pixels = wct.GetPixels32();
            cpuTex.SetPixels32(pixels);
            cpuTex.Apply(false);
        }
        else
        {
            if (statusText) statusText.text = $"Unsupported texture type: {src.GetType().Name}";
            return;
        }

        var colors = cpuTex.GetPixels32();
        if (colors == null || colors.Length == 0)
        {
            if (statusText) statusText.text = "Empty pixel buffer";
            return;
        }

        int need = colors.Length * 4;
        if (rgbaBuffer == null || rgbaBuffer.Length != need)
            rgbaBuffer = new byte[need];

        for (int i = 0, j = 0; i < colors.Length; i++)
        {
            Color32 c = colors[i];
            rgbaBuffer[j++] = c.r;
            rgbaBuffer[j++] = c.g;
            rgbaBuffer[j++] = c.b;
            rgbaBuffer[j++] = c.a;
        }

        var result = reader.Decode(
            rgbaBuffer,
            w,
            h,
            RGBLuminanceSource.BitmapFormat.RGBA32
        );

        if (result == null)
        {
            // Important:
            // If no QR is visible, we allow the same card to be accepted again later.
            // This prevents repeated triggering while the same card remains in view,
            // but still allows reuse after the card has been moved away.
            if (waitingForCardToClear)
            {
                waitingForCardToClear = false;
                lastAcceptedPayload = "";
                Debug.Log("[QRScanner] Card cleared. Scanner is ready for the next QR code.");
            }

            if (statusText) statusText.text = $"No QR decoded ({w}x{h})";
            return;
        }

        string txt = result.Text ?? string.Empty;
        Debug.Log($"[QRScanner] format={result.BarcodeFormat}, text=<{txt}>, len={txt.Length}, size={w}x{h}");

        // 空内容一律视为无效，不触发成功逻辑
        if (string.IsNullOrWhiteSpace(txt))
        {
            if (statusText) statusText.text = $"QR detected but payload empty ({result.BarcodeFormat})";
            return;
        }

        // 只接受像 thesis 卡片 JSON 的内容，避免误报
        string trimmed = txt.TrimStart();
        if (!(trimmed.StartsWith("{") && txt.Contains("gesture")))
        {
            if (statusText) statusText.text = "Decoded non-card content";
            Debug.Log($"[QRScanner] Rejected payload=<{txt}>");
            return;
        }

        // Prevent immediately accepting the same still-visible QR code again
        if (requireClearBeforeSameCode && waitingForCardToClear && txt == lastAcceptedPayload)
        {
            if (statusText) statusText.text = "Same card still visible. Move to the next QR card.";
            return;
        }

        AcceptDecodedPayload(txt);
    }

    void AcceptDecodedPayload(string txt)
    {
        scanning = false;
        lastAcceptedPayload = txt;
        waitingForCardToClear = true;

        if (beep) AudioSource.PlayClipAtPoint(beep, Vector3.zero, 0.6f);
        Handheld.Vibrate();

        onDecoded?.Invoke(txt);
        ShowDecodedSummary(txt);

        if (openUrlIfLink && Uri.IsWellFormedUriString(txt, UriKind.Absolute))
        {
            Application.OpenURL(txt);
        }

        if (autoRestartAfterDecode)
        {
            if (restartCoroutine != null)
                StopCoroutine(restartCoroutine);

            restartCoroutine = StartCoroutine(AutoRestartScanDelayed());
        }
    }

    IEnumerator AutoRestartScanDelayed()
    {
        yield return new WaitForSeconds(autoRestartDelay);

        scanning = true;
        nextScan = Time.time + 0.1f;

        if (statusText)
            statusText.text = "Scan the next card";

        Debug.Log("[QRScanner] Auto restart enabled. Ready for next QR card.");
        restartCoroutine = null;
    }

    Texture2D EnsureScratchTexture(int w, int h)
    {
        if (scratchTex == null || scratchTex.width != w || scratchTex.height != h)
        {
            if (scratchTex != null)
                Destroy(scratchTex);

            scratchTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            scratchTex.wrapMode = TextureWrapMode.Clamp;
            scratchTex.filterMode = FilterMode.Bilinear;
        }
        return scratchTex;
    }

    void ShowDecodedSummary(string txt)
    {
        if (!statusText) return;

        try
        {
            var cfg = JsonUtility.FromJson<CardConfig>(txt);
            if (!string.IsNullOrEmpty(cfg?.card_id))
            {
                string g = string.IsNullOrEmpty(cfg.gesture) ? "gesture" : cfg.gesture;
                statusText.text = $"Card {cfg.card_id}  •  {g}  •  {cfg.hold_secs:0.0} s";
                return;
            }
        }
        catch
        {
            // ignore
        }

        if (Uri.IsWellFormedUriString(txt, UriKind.Absolute))
        {
            statusText.text = "Opening link…";
        }
        else
        {
            statusText.text = "QR code scanned";
        }
    }

    public void RestartScan()
    {
        scanning = true;
        nextScan = Time.time + 0.1f;

        if (statusText)
            statusText.text = "Scan the next card";

        Debug.Log("[QRScanner] Manual restart scan.");
    }

    public void ForceResetScanner()
    {
        scanning = true;
        waitingForCardToClear = false;
        lastAcceptedPayload = "";
        nextScan = Time.time + 0.1f;

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }

        if (statusText)
            statusText.text = "Scanner reset. Scan a QR card.";

        Debug.Log("[QRScanner] Force reset scanner.");
    }

    void OnDestroy()
    {
        if (scratchTex != null)
            Destroy(scratchTex);

        if (restartCoroutine != null)
        {
            StopCoroutine(restartCoroutine);
            restartCoroutine = null;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ClearHideFlags(this);
        ClearHideFlags(preview);
        ClearHideFlags(statusText);
        ClearHideFlags(fitter);
        ClearHideFlags(cameraTap);

        if (fitter == null && preview != null)
            fitter = preview.GetComponent<AspectRatioFitter>();
    }

    static void ClearHideFlags(UnityEngine.Object o)
    {
        if (o != null) o.hideFlags = HideFlags.None;
    }
#endif
}