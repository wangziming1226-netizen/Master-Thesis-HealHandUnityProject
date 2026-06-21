using System;
using UnityEngine;

public class DirectHandRetargetBridge : MonoBehaviour
{
    [Serializable]
    public class BoneSegment
    {
        public string name;
        public Transform bone;
        public int landmarkStart;
        public int landmarkEnd;
        public float smooth = 12f;

        [HideInInspector] public Quaternion bindWorldRotation;
        [HideInInspector] public Vector3 bindSourceDirection;
    }

    [Header("References")]
    public HandLandmarkCache landmarkCache;

    [Header("Auto Find")]
    public bool autoFindLandmarkCache = true;
    public float refindInterval = 1f;

    [Header("Palm / Wrist")]
    public bool drivePalmRoot = true;
    public float palmSmooth = 10f;
    public bool invertPalmNormal = false;

    [Tooltip("Press this key to recalibrate. Ideally, keep your hand close to the model's default pose when doing so.")]
    public KeyCode recalibrateKey = KeyCode.C;

    [Tooltip("Automatically calibrate once when valid landmarks are first detected.")]
    public bool autoCalibrateOnFirstValidFrame = true;

    [Header("Index Finger Retarget")]
    public BoneSegment index1 = new BoneSegment { name = "Index Proximal", landmarkStart = 5, landmarkEnd = 6, smooth = 14f };
    public BoneSegment index2 = new BoneSegment { name = "Index Middle",   landmarkStart = 6, landmarkEnd = 7, smooth = 14f };
    public BoneSegment index3 = new BoneSegment { name = "Index Distal",   landmarkStart = 7, landmarkEnd = 8, smooth = 14f };

    [Header("Debug")]
    public bool calibrated = false;
    public Quaternion bindPalmSourceRotation;
    public Quaternion currentPalmSourceRotation;

    private float nextRefindTime = 0f;
    private Quaternion bindPivotLocalRotation;

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
        if (autoFindLandmarkCache && landmarkCache == null && Time.time >= nextRefindTime)
        {
            TryResolveReferences();
            nextRefindTime = Time.time + refindInterval;
        }

        if (landmarkCache == null || !landmarkCache.HasValidData)
            return;

        Vector3[] pts = landmarkCache.GetLandmarksCopy();

        if (!calibrated && autoCalibrateOnFirstValidFrame)
        {
            CalibrateFromCurrentPose(pts);
        }

        if (Input.GetKeyDown(recalibrateKey))
        {
            CalibrateFromCurrentPose(pts);
            Debug.Log("[DirectHandRetargetBridge] Recalibrated.");
        }

        if (!calibrated)
            return;

        if (drivePalmRoot)
        {
            ApplyPalmRotation(pts);
        }

        ApplySegment(index1, pts);
        ApplySegment(index2, pts);
        ApplySegment(index3, pts);
    }

    private void TryResolveReferences()
    {
        if (landmarkCache == null)
        {
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
    }

    [ContextMenu("Calibrate Now (if cache valid)")]
    public void CalibrateNowFromRuntime()
    {
        if (landmarkCache == null || !landmarkCache.HasValidData)
        {
            Debug.LogWarning("[DirectHandRetargetBridge] Cannot calibrate: landmark cache invalid.");
            return;
        }

        CalibrateFromCurrentPose(landmarkCache.GetLandmarksCopy());
        Debug.Log("[DirectHandRetargetBridge] Calibrated from runtime.");
    }

    private void CalibrateFromCurrentPose(Vector3[] pts)
    {
        if (pts == null || pts.Length < 21)
            return;

        bindPivotLocalRotation = transform.localRotation;
        bindPalmSourceRotation = ComputePalmSourceRotation(pts);

        CacheSegment(index1, pts);
        CacheSegment(index2, pts);
        CacheSegment(index3, pts);

        calibrated = true;
    }

    private void CacheSegment(BoneSegment seg, Vector3[] pts)
    {
        if (seg == null || seg.bone == null)
            return;

        seg.bindWorldRotation = seg.bone.rotation;
        seg.bindSourceDirection = GetDirectionFromLandmarks(pts, seg.landmarkStart, seg.landmarkEnd);
    }

    private void ApplyPalmRotation(Vector3[] pts)
    {
        currentPalmSourceRotation = ComputePalmSourceRotation(pts);

        Quaternion delta = currentPalmSourceRotation * Quaternion.Inverse(bindPalmSourceRotation);
        Quaternion targetLocalRotation = delta * bindPivotLocalRotation;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetLocalRotation,
            Time.deltaTime * palmSmooth
        );
    }

    private void ApplySegment(BoneSegment seg, Vector3[] pts)
    {
        if (seg == null || seg.bone == null)
            return;

        Vector3 currentDir = GetDirectionFromLandmarks(pts, seg.landmarkStart, seg.landmarkEnd);
        if (currentDir.sqrMagnitude < 1e-8f || seg.bindSourceDirection.sqrMagnitude < 1e-8f)
            return;

        Quaternion delta = Quaternion.FromToRotation(seg.bindSourceDirection, currentDir);
        Quaternion targetWorldRotation = delta * seg.bindWorldRotation;

        seg.bone.rotation = Quaternion.Slerp(
            seg.bone.rotation,
            targetWorldRotation,
            Time.deltaTime * seg.smooth
        );
    }

    private Vector3 GetDirectionFromLandmarks(Vector3[] pts, int a, int b)
    {
        if (pts == null || pts.Length <= Mathf.Max(a, b))
            return Vector3.forward;

        Vector3 dir = pts[b] - pts[a];
        if (dir.sqrMagnitude < 1e-8f)
            return Vector3.forward;

        return dir.normalized;
    }

    private Quaternion ComputePalmSourceRotation(Vector3[] pts)
    {
        // MediaPipe:
        // 0 wrist
        // 5 index MCP
        // 9 middle MCP
        // 17 pinky MCP

        Vector3 wrist = pts[0];
        Vector3 indexMcp = pts[5];
        Vector3 middleMcp = pts[9];
        Vector3 pinkyMcp = pts[17];

        Vector3 fingerUp = (middleMcp - wrist).normalized;
        Vector3 across = (pinkyMcp - indexMcp).normalized;

        Vector3 normal = Vector3.Cross(across, fingerUp).normalized;
        if (invertPalmNormal)
            normal = -normal;

        if (normal.sqrMagnitude < 1e-8f)
            normal = Vector3.forward;

        if (fingerUp.sqrMagnitude < 1e-8f)
            fingerUp = Vector3.up;

        return Quaternion.LookRotation(normal, fingerUp);
    }
}
