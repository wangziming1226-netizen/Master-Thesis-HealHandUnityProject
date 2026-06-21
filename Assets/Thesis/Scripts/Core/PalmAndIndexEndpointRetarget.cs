using System;
using UnityEngine;

public class PalmAndIndexEndpointRetarget : MonoBehaviour
{
    public enum CaptureFinger
    {
        Thumb,
        Index,
        Middle,
        Ring,
        Pinky
    }

    [Header("References")]
    public HandLandmarkCache landmarkCache;

    [Header("Auto Find")]
    public bool autoFindLandmarkCache = true;
    public float refindInterval = 1f;

    [Header("Palm Root")]
    public bool drivePalmRoot = true;
    public float palmSmooth = 10f;
    public bool invertPalmNormal = false;

    [Header("Capture Target")]
    public CaptureFinger captureFinger = CaptureFinger.Index;

    [Header("Runtime Keys")]
    public KeyCode calibratePalmKey = KeyCode.C;
    public KeyCode captureOpenKey = KeyCode.O;
    public KeyCode captureFistKey = KeyCode.F;
    public KeyCode captureOkKey = KeyCode.K;

    [Header("Finger Smooth")]
    public float fingerSmooth = 14f;

    [Header("Raw Curl Mapping")]
    public float straightAngle = 175f;
    public float curledAngle = 65f;

    [Header("Thumb Raw Mapping (Distance-Based)")]
    [Tooltip("Normalised distance threshold when the thumb tip is close to the palm.")]
    public float thumbNearDistance = 0.25f;

    [Tooltip("Normalised distance threshold when the thumb tip is far from the palm.")]
    public float thumbFarDistance = 1.10f;

    [Header("OK / Pinch Detection")]
    [Tooltip("pinchRaw increases when the thumb tip is close to the index fingertip.")]
    public float pinchNearDistance = 0.06f;

    [Tooltip("pinchRaw decreases when the thumb tip is far from the index fingertip.")]
    public float pinchFarDistance = 0.40f;

    [Tooltip("The more extended the other three fingers are, the more easily the hand is recognised as an OK gesture.")]
    public float okOtherOpenMin = 0.35f;

    [Tooltip("Smoothing applied to the OK pose blend.")]
    public float okBlendSmooth = 10f;

    [Header("Debug")]
    public bool palmCalibrated = false;
    [Range(0f, 1f)] public float currentPinchRaw = 0f;
    [Range(0f, 1f)] public float currentOtherOpenGate = 0f;
    [Range(0f, 1f)] public float okBlend = 0f;

    // =========================
    // Thumb
    // =========================
    [Header("Thumb Bones")]
    public Transform thumbBone1;
    public Transform thumbBone2;
    public Transform thumbBone3;

    public bool openCapturedThumb = false;
    public bool fistCapturedThumb = false;
    public bool okCapturedThumb = false;

    [Range(0f, 1f)] public float currentRawThumb = 0f;
    [Range(0f, 1f)] public float normalizedThumb = 0f;

    public float openRawThumb = 0f;
    public float fistRawThumb = 1f;

    [SerializeField] private Quaternion openRotThumb1;
    [SerializeField] private Quaternion openRotThumb2;
    [SerializeField] private Quaternion openRotThumb3;

    [SerializeField] private Quaternion fistRotThumb1;
    [SerializeField] private Quaternion fistRotThumb2;
    [SerializeField] private Quaternion fistRotThumb3;

    [SerializeField] private Quaternion okRotThumb1;
    [SerializeField] private Quaternion okRotThumb2;
    [SerializeField] private Quaternion okRotThumb3;

    // =========================
    // Index (keep the original field names to preserve existing serialized data)
    // =========================
    [Header("Index Bones")]
    public Transform indexBone1;
    public Transform indexBone2;
    public Transform indexBone3;

    public bool openCaptured = false;
    public bool fistCaptured = false;
    public bool okCapturedIndex = false;

    [Range(0f, 1f)] public float currentRawIndex = 0f;
    [Range(0f, 1f)] public float normalizedIndex = 0f;

    public float openRawIndex = 0f;
    public float fistRawIndex = 1f;

    [SerializeField] private Quaternion openRot1;
    [SerializeField] private Quaternion openRot2;
    [SerializeField] private Quaternion openRot3;

    [SerializeField] private Quaternion fistRot1;
    [SerializeField] private Quaternion fistRot2;
    [SerializeField] private Quaternion fistRot3;

    [SerializeField] private Quaternion okRotIndex1;
    [SerializeField] private Quaternion okRotIndex2;
    [SerializeField] private Quaternion okRotIndex3;

    // =========================
    // Middle
    // =========================
    [Header("Middle Bones")]
    public Transform middleBone1;
    public Transform middleBone2;
    public Transform middleBone3;

    public bool openCapturedMiddle = false;
    public bool fistCapturedMiddle = false;

    [Range(0f, 1f)] public float currentRawMiddle = 0f;
    [Range(0f, 1f)] public float normalizedMiddle = 0f;

    public float openRawMiddle = 0f;
    public float fistRawMiddle = 1f;

    [SerializeField] private Quaternion openRotMiddle1;
    [SerializeField] private Quaternion openRotMiddle2;
    [SerializeField] private Quaternion openRotMiddle3;

    [SerializeField] private Quaternion fistRotMiddle1;
    [SerializeField] private Quaternion fistRotMiddle2;
    [SerializeField] private Quaternion fistRotMiddle3;

    // =========================
    // Ring
    // =========================
    [Header("Ring Bones")]
    public Transform ringBone1;
    public Transform ringBone2;
    public Transform ringBone3;

    public bool openCapturedRing = false;
    public bool fistCapturedRing = false;

    [Range(0f, 1f)] public float currentRawRing = 0f;
    [Range(0f, 1f)] public float normalizedRing = 0f;

    public float openRawRing = 0f;
    public float fistRawRing = 1f;

    [SerializeField] private Quaternion openRotRing1;
    [SerializeField] private Quaternion openRotRing2;
    [SerializeField] private Quaternion openRotRing3;

    [SerializeField] private Quaternion fistRotRing1;
    [SerializeField] private Quaternion fistRotRing2;
    [SerializeField] private Quaternion fistRotRing3;

    // =========================
    // Pinky
    // =========================
    [Header("Pinky Bones")]
    public Transform pinkyBone1;
    public Transform pinkyBone2;
    public Transform pinkyBone3;

    public bool openCapturedPinky = false;
    public bool fistCapturedPinky = false;

    [Range(0f, 1f)] public float currentRawPinky = 0f;
    [Range(0f, 1f)] public float normalizedPinky = 0f;

    public float openRawPinky = 0f;
    public float fistRawPinky = 1f;

    [SerializeField] private Quaternion openRotPinky1;
    [SerializeField] private Quaternion openRotPinky2;
    [SerializeField] private Quaternion openRotPinky3;

    [SerializeField] private Quaternion fistRotPinky1;
    [SerializeField] private Quaternion fistRotPinky2;
    [SerializeField] private Quaternion fistRotPinky3;

    private Quaternion bindRootLocalRotation;
    private Quaternion bindPalmSourceRotation;
    private float nextRefindTime = 0f;
    private float smoothedOkBlend = 0f;

    private void Awake()
    {
        TryResolveReferences();
    }

    private void OnEnable()
    {
        TryResolveReferences();
    }

    private void Update()
    {
        if (landmarkCache == null && Time.time >= nextRefindTime)
        {
            TryResolveReferences();
            nextRefindTime = Time.time + refindInterval;
        }

        if (landmarkCache == null || !landmarkCache.HasValidData)
            return;

        Vector3[] pts = landmarkCache.GetLandmarksCopy();

        if (Input.GetKeyDown(calibratePalmKey))
        {
            CalibratePalm(pts);
            Debug.Log("[PalmAndIndexEndpointRetarget] Palm calibrated.");
        }

        if (Input.GetKeyDown(captureOpenKey))
        {
            CaptureOpenEndpoint(captureFinger, pts);
            Debug.Log($"[PalmAndIndexEndpointRetarget] Open endpoint captured for {captureFinger}.");
        }

        if (Input.GetKeyDown(captureFistKey))
        {
            CaptureFistEndpoint(captureFinger, pts);
            Debug.Log($"[PalmAndIndexEndpointRetarget] Fist endpoint captured for {captureFinger}.");
        }

        if (Input.GetKeyDown(captureOkKey))
        {
            CaptureOkEndpoint(captureFinger);
            Debug.Log($"[PalmAndIndexEndpointRetarget] OK endpoint captured for {captureFinger}.");
        }

        // Update raw values each frame.
        currentRawThumb  = ComputeThumbRaw(pts);
        currentRawIndex  = ComputeFingerRaw(pts, 5, 6, 7, 8);
        currentRawMiddle = ComputeFingerRaw(pts, 9, 10, 11, 12);
        currentRawRing   = ComputeFingerRaw(pts, 13, 14, 15, 16);
        currentRawPinky  = ComputeFingerRaw(pts, 17, 18, 19, 20);

        if (drivePalmRoot && palmCalibrated)
        {
            ApplyPalm(pts);
        }

        // First calculate the baseline normalised values for the thumb and four fingers.
        if (openCapturedThumb && fistCapturedThumb)
            normalizedThumb = NormalizeByObservedRange(currentRawThumb, openRawThumb, fistRawThumb);

        if (openCaptured && fistCaptured)
            normalizedIndex = NormalizeByObservedRange(currentRawIndex, openRawIndex, fistRawIndex);

        if (openCapturedMiddle && fistCapturedMiddle)
            normalizedMiddle = NormalizeByObservedRange(currentRawMiddle, openRawMiddle, fistRawMiddle);

        if (openCapturedRing && fistCapturedRing)
            normalizedRing = NormalizeByObservedRange(currentRawRing, openRawRing, fistRawRing);

        if (openCapturedPinky && fistCapturedPinky)
            normalizedPinky = NormalizeByObservedRange(currentRawPinky, openRawPinky, fistRawPinky);

        // OK / pinch blend.
        currentPinchRaw = ComputePinchRaw(pts);
        currentOtherOpenGate = ComputeOtherOpenGate(normalizedMiddle, normalizedRing, normalizedPinky);
        okBlend = Mathf.Clamp01(currentPinchRaw * currentOtherOpenGate);
        smoothedOkBlend = Mathf.Lerp(smoothedOkBlend, okBlend, Time.deltaTime * okBlendSmooth);

        // Thumb
        if (openCapturedThumb && fistCapturedThumb)
        {
            Quaternion baseThumb1 = Quaternion.Slerp(openRotThumb1, fistRotThumb1, normalizedThumb);
            Quaternion baseThumb2 = Quaternion.Slerp(openRotThumb2, fistRotThumb2, normalizedThumb);
            Quaternion baseThumb3 = Quaternion.Slerp(openRotThumb3, fistRotThumb3, normalizedThumb);

            if (okCapturedThumb)
            {
                baseThumb1 = Quaternion.Slerp(baseThumb1, okRotThumb1, smoothedOkBlend);
                baseThumb2 = Quaternion.Slerp(baseThumb2, okRotThumb2, smoothedOkBlend);
                baseThumb3 = Quaternion.Slerp(baseThumb3, okRotThumb3, smoothedOkBlend);
            }

            ApplyFingerToTargetRotations(thumbBone1, thumbBone2, thumbBone3, baseThumb1, baseThumb2, baseThumb3);
        }

        // Index
        if (openCaptured && fistCaptured)
        {
            Quaternion baseIndex1 = Quaternion.Slerp(openRot1, fistRot1, normalizedIndex);
            Quaternion baseIndex2 = Quaternion.Slerp(openRot2, fistRot2, normalizedIndex);
            Quaternion baseIndex3 = Quaternion.Slerp(openRot3, fistRot3, normalizedIndex);

            if (okCapturedIndex)
            {
                baseIndex1 = Quaternion.Slerp(baseIndex1, okRotIndex1, smoothedOkBlend);
                baseIndex2 = Quaternion.Slerp(baseIndex2, okRotIndex2, smoothedOkBlend);
                baseIndex3 = Quaternion.Slerp(baseIndex3, okRotIndex3, smoothedOkBlend);
            }

            ApplyFingerToTargetRotations(indexBone1, indexBone2, indexBone3, baseIndex1, baseIndex2, baseIndex3);
        }

        // Middle
        if (openCapturedMiddle && fistCapturedMiddle)
        {
            ApplyFingerFromEndpoints(
                middleBone1, middleBone2, middleBone3,
                openRotMiddle1, openRotMiddle2, openRotMiddle3,
                fistRotMiddle1, fistRotMiddle2, fistRotMiddle3,
                normalizedMiddle
            );
        }

        // Ring
        if (openCapturedRing && fistCapturedRing)
        {
            ApplyFingerFromEndpoints(
                ringBone1, ringBone2, ringBone3,
                openRotRing1, openRotRing2, openRotRing3,
                fistRotRing1, fistRotRing2, fistRotRing3,
                normalizedRing
            );
        }

        // Pinky
        if (openCapturedPinky && fistCapturedPinky)
        {
            ApplyFingerFromEndpoints(
                pinkyBone1, pinkyBone2, pinkyBone3,
                openRotPinky1, openRotPinky2, openRotPinky3,
                fistRotPinky1, fistRotPinky2, fistRotPinky3,
                normalizedPinky
            );
        }
    }

    private void TryResolveReferences()
    {
        if (!autoFindLandmarkCache || landmarkCache != null)
            return;

        landmarkCache = FindFirstObjectByType<HandLandmarkCache>();

        if (landmarkCache == null)
        {
            HandLandmarkCache[] all = Resources.FindObjectsOfTypeAll<HandLandmarkCache>();
            foreach (var item in all)
            {
                if (item == null) continue;
                if (!item.gameObject.scene.isLoaded) continue;
                landmarkCache = item;
                break;
            }
        }
    }

    private void CalibratePalm(Vector3[] pts)
    {
        bindRootLocalRotation = transform.localRotation;
        bindPalmSourceRotation = ComputePalmSourceRotation(pts);
        palmCalibrated = true;
    }

    private void CaptureOpenEndpoint(CaptureFinger finger, Vector3[] pts)
    {
        switch (finger)
        {
            case CaptureFinger.Thumb:
                if (thumbBone1 != null) openRotThumb1 = thumbBone1.localRotation;
                if (thumbBone2 != null) openRotThumb2 = thumbBone2.localRotation;
                if (thumbBone3 != null) openRotThumb3 = thumbBone3.localRotation;
                openRawThumb = ComputeThumbRaw(pts);
                openCapturedThumb = true;
                break;

            case CaptureFinger.Index:
                if (indexBone1 != null) openRot1 = indexBone1.localRotation;
                if (indexBone2 != null) openRot2 = indexBone2.localRotation;
                if (indexBone3 != null) openRot3 = indexBone3.localRotation;
                openRawIndex = ComputeFingerRaw(pts, 5, 6, 7, 8);
                openCaptured = true;
                break;

            case CaptureFinger.Middle:
                if (middleBone1 != null) openRotMiddle1 = middleBone1.localRotation;
                if (middleBone2 != null) openRotMiddle2 = middleBone2.localRotation;
                if (middleBone3 != null) openRotMiddle3 = middleBone3.localRotation;
                openRawMiddle = ComputeFingerRaw(pts, 9, 10, 11, 12);
                openCapturedMiddle = true;
                break;

            case CaptureFinger.Ring:
                if (ringBone1 != null) openRotRing1 = ringBone1.localRotation;
                if (ringBone2 != null) openRotRing2 = ringBone2.localRotation;
                if (ringBone3 != null) openRotRing3 = ringBone3.localRotation;
                openRawRing = ComputeFingerRaw(pts, 13, 14, 15, 16);
                openCapturedRing = true;
                break;

            case CaptureFinger.Pinky:
                if (pinkyBone1 != null) openRotPinky1 = pinkyBone1.localRotation;
                if (pinkyBone2 != null) openRotPinky2 = pinkyBone2.localRotation;
                if (pinkyBone3 != null) openRotPinky3 = pinkyBone3.localRotation;
                openRawPinky = ComputeFingerRaw(pts, 17, 18, 19, 20);
                openCapturedPinky = true;
                break;
        }
    }

    private void CaptureFistEndpoint(CaptureFinger finger, Vector3[] pts)
    {
        switch (finger)
        {
            case CaptureFinger.Thumb:
                if (thumbBone1 != null) fistRotThumb1 = thumbBone1.localRotation;
                if (thumbBone2 != null) fistRotThumb2 = thumbBone2.localRotation;
                if (thumbBone3 != null) fistRotThumb3 = thumbBone3.localRotation;
                fistRawThumb = ComputeThumbRaw(pts);
                fistCapturedThumb = true;
                break;

            case CaptureFinger.Index:
                if (indexBone1 != null) fistRot1 = indexBone1.localRotation;
                if (indexBone2 != null) fistRot2 = indexBone2.localRotation;
                if (indexBone3 != null) fistRot3 = indexBone3.localRotation;
                fistRawIndex = ComputeFingerRaw(pts, 5, 6, 7, 8);
                fistCaptured = true;
                break;

            case CaptureFinger.Middle:
                if (middleBone1 != null) fistRotMiddle1 = middleBone1.localRotation;
                if (middleBone2 != null) fistRotMiddle2 = middleBone2.localRotation;
                if (middleBone3 != null) fistRotMiddle3 = middleBone3.localRotation;
                fistRawMiddle = ComputeFingerRaw(pts, 9, 10, 11, 12);
                fistCapturedMiddle = true;
                break;

            case CaptureFinger.Ring:
                if (ringBone1 != null) fistRotRing1 = ringBone1.localRotation;
                if (ringBone2 != null) fistRotRing2 = ringBone2.localRotation;
                if (ringBone3 != null) fistRotRing3 = ringBone3.localRotation;
                fistRawRing = ComputeFingerRaw(pts, 13, 14, 15, 16);
                fistCapturedRing = true;
                break;

            case CaptureFinger.Pinky:
                if (pinkyBone1 != null) fistRotPinky1 = pinkyBone1.localRotation;
                if (pinkyBone2 != null) fistRotPinky2 = pinkyBone2.localRotation;
                if (pinkyBone3 != null) fistRotPinky3 = pinkyBone3.localRotation;
                fistRawPinky = ComputeFingerRaw(pts, 17, 18, 19, 20);
                fistCapturedPinky = true;
                break;
        }
    }

    private void CaptureOkEndpoint(CaptureFinger finger)
    {
        switch (finger)
        {
            case CaptureFinger.Thumb:
                if (thumbBone1 != null) okRotThumb1 = thumbBone1.localRotation;
                if (thumbBone2 != null) okRotThumb2 = thumbBone2.localRotation;
                if (thumbBone3 != null) okRotThumb3 = thumbBone3.localRotation;
                okCapturedThumb = true;
                break;

            case CaptureFinger.Index:
                if (indexBone1 != null) okRotIndex1 = indexBone1.localRotation;
                if (indexBone2 != null) okRotIndex2 = indexBone2.localRotation;
                if (indexBone3 != null) okRotIndex3 = indexBone3.localRotation;
                okCapturedIndex = true;
                break;
        }
    }

    private void ApplyPalm(Vector3[] pts)
    {
        Quaternion currentPalmSourceRotation = ComputePalmSourceRotation(pts);
        Quaternion delta = currentPalmSourceRotation * Quaternion.Inverse(bindPalmSourceRotation);
        Quaternion targetLocalRotation = delta * bindRootLocalRotation;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetLocalRotation,
            Time.deltaTime * palmSmooth
        );
    }

    private void ApplyFingerFromEndpoints(
        Transform bone1, Transform bone2, Transform bone3,
        Quaternion open1, Quaternion open2, Quaternion open3,
        Quaternion fist1, Quaternion fist2, Quaternion fist3,
        float t)
    {
        if (bone1 != null)
        {
            Quaternion target = Quaternion.Slerp(open1, fist1, t);
            bone1.localRotation = Quaternion.Slerp(bone1.localRotation, target, Time.deltaTime * fingerSmooth);
        }

        if (bone2 != null)
        {
            Quaternion target = Quaternion.Slerp(open2, fist2, t);
            bone2.localRotation = Quaternion.Slerp(bone2.localRotation, target, Time.deltaTime * fingerSmooth);
        }

        if (bone3 != null)
        {
            Quaternion target = Quaternion.Slerp(open3, fist3, t);
            bone3.localRotation = Quaternion.Slerp(bone3.localRotation, target, Time.deltaTime * fingerSmooth);
        }
    }

    private void ApplyFingerToTargetRotations(
        Transform bone1, Transform bone2, Transform bone3,
        Quaternion target1, Quaternion target2, Quaternion target3)
    {
        if (bone1 != null)
            bone1.localRotation = Quaternion.Slerp(bone1.localRotation, target1, Time.deltaTime * fingerSmooth);

        if (bone2 != null)
            bone2.localRotation = Quaternion.Slerp(bone2.localRotation, target2, Time.deltaTime * fingerSmooth);

        if (bone3 != null)
            bone3.localRotation = Quaternion.Slerp(bone3.localRotation, target3, Time.deltaTime * fingerSmooth);
    }

    private float ComputeThumbRaw(Vector3[] pts)
    {
        // 0 wrist, 4 thumb tip, 5 index MCP, 9 middle MCP, 17 pinky MCP
        Vector3 wrist = pts[0];
        Vector3 indexMcp = pts[5];
        Vector3 middleMcp = pts[9];
        Vector3 pinkyMcp = pts[17];
        Vector3 thumbTip = pts[4];

        // Palm centre.
        Vector3 palmCenter = (wrist + indexMcp + middleMcp + pinkyMcp) * 0.25f;

        // Normalise by palm width to reduce the effect of hand distance from the camera.
        float palmWidth = Vector3.Distance(indexMcp, pinkyMcp);
        palmWidth = Mathf.Max(palmWidth, 1e-5f);

        float tipToPalm = Vector3.Distance(thumbTip, palmCenter) / palmWidth;

        // Open hand: a farther thumb tip produces a raw value closer to 0.
        // Fist: a closer thumb tip produces a raw value closer to 1.
        float raw = 1f - Mathf.Clamp01(Mathf.InverseLerp(thumbNearDistance, thumbFarDistance, tipToPalm));

        return raw;
    }

    private float ComputeFingerRaw(Vector3[] pts, int mcp, int pip, int dip, int tip)
    {
        float angleMcp = JointAngle(pts[0], pts[mcp], pts[pip]);
        float anglePip = JointAngle(pts[mcp], pts[pip], pts[dip]);
        float angleDip = JointAngle(pts[pip], pts[dip], pts[tip]);

        float curlMcp = AngleToCurl(angleMcp, straightAngle, curledAngle);
        float curlPip = AngleToCurl(anglePip, straightAngle, curledAngle);
        float curlDip = AngleToCurl(angleDip, straightAngle, curledAngle);

        return Mathf.Clamp01(curlMcp * 0.2f + curlPip * 0.5f + curlDip * 0.3f);
    }

    private float ComputePinchRaw(Vector3[] pts)
    {
        Vector3 thumbTip = pts[4];
        Vector3 indexTip = pts[8];
        Vector3 indexMcp = pts[5];
        Vector3 pinkyMcp = pts[17];

        float palmWidth = Vector3.Distance(indexMcp, pinkyMcp);
        palmWidth = Mathf.Max(palmWidth, 1e-5f);

        float tipDist = Vector3.Distance(thumbTip, indexTip) / palmWidth;

        // The closer the two fingertips are, the closer pinchRaw is to 1.
        float raw = 1f - Mathf.Clamp01(Mathf.InverseLerp(pinchNearDistance, pinchFarDistance, tipDist));
        return raw;
    }

    private float ComputeOtherOpenGate(float middleNorm, float ringNorm, float pinkyNorm)
    {
        float otherOpen = 1f - ((middleNorm + ringNorm + pinkyNorm) / 3f);
        return Mathf.Clamp01(Mathf.InverseLerp(okOtherOpenMin, 1f, otherOpen));
    }

    private float NormalizeByObservedRange(float raw, float openRaw, float fistRaw)
    {
        if (Mathf.Approximately(openRaw, fistRaw))
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(openRaw, fistRaw, raw));
    }

    private Quaternion ComputePalmSourceRotation(Vector3[] pts)
    {
        Vector3 wrist = pts[0];
        Vector3 indexMcp = pts[5];
        Vector3 middleMcp = pts[9];
        Vector3 pinkyMcp = pts[17];

        Vector3 fingerUp = (middleMcp - wrist).normalized;
        Vector3 across = (pinkyMcp - indexMcp).normalized;

        Vector3 normal = Vector3.Cross(across, fingerUp).normalized;
        if (invertPalmNormal)
            normal = -normal;

        if (normal.sqrMagnitude < 1e-8f) normal = Vector3.forward;
        if (fingerUp.sqrMagnitude < 1e-8f) fingerUp = Vector3.up;

        return Quaternion.LookRotation(normal, fingerUp);
    }

    private float JointAngle(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ba = (a - b).normalized;
        Vector3 bc = (c - b).normalized;
        float dot = Mathf.Clamp(Vector3.Dot(ba, bc), -1f, 1f);
        return Mathf.Acos(dot) * Mathf.Rad2Deg;
    }

    private float AngleToCurl(float angle, float straightAngle, float curledAngle)
    {
        if (Mathf.Approximately(straightAngle, curledAngle))
            return 0f;

        float t = (straightAngle - angle) / (straightAngle - curledAngle);
        return Mathf.Clamp01(t);
    }
}