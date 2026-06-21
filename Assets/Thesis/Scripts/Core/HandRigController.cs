using UnityEngine;

public class HandRigController : MonoBehaviour
{
    [System.Serializable]
    public class FingerChain
    {
        public string name;
        public Transform bone1;
        public Transform bone2;
        public Transform bone3;

        [Header("Curl Angles")]
        public float angle1 = 15f;
        public float angle2 = 45f;
        public float angle3 = 25f;

        [Header("Axis Per Bone")]
        public Vector3 axis1 = Vector3.right;
        public Vector3 axis2 = Vector3.right;
        public Vector3 axis3 = Vector3.right;

        [HideInInspector] public Quaternion bone1Start;
        [HideInInspector] public Quaternion bone2Start;
        [HideInInspector] public Quaternion bone3Start;
    }

    [Header("Finger Chains")]
    public FingerChain thumb;
    public FingerChain index;
    public FingerChain middle;
    public FingerChain ring;
    public FingerChain pinky;

    [Header("Debug Preview")]
    public bool debugPreview = true;

    [Range(0f, 1f)] public float thumbCurl = 0f;
    [Range(0f, 1f)] public float indexCurl = 0f;
    [Range(0f, 1f)] public float middleCurl = 0f;
    [Range(0f, 1f)] public float ringCurl = 0f;
    [Range(0f, 1f)] public float pinkyCurl = 0f;

    private bool cached = false;

    private void Awake()
    {
        CacheAllStartRotations();
    }

    private void OnEnable()
    {
        if (!cached) CacheAllStartRotations();
    }

    private void Update()
    {
        if (!debugPreview) return;

        SetFingerCurls(thumbCurl, indexCurl, middleCurl, ringCurl, pinkyCurl);
    }

    [ContextMenu("Cache Start Rotations")]
    public void CacheAllStartRotations()
    {
        CacheStartRotation(thumb);
        CacheStartRotation(index);
        CacheStartRotation(middle);
        CacheStartRotation(ring);
        CacheStartRotation(pinky);
        cached = true;
    }

    [ContextMenu("Reset To Start Pose")]
    public void ResetToStartPose()
    {
        ResetFinger(thumb);
        ResetFinger(index);
        ResetFinger(middle);
        ResetFinger(ring);
        ResetFinger(pinky);

        thumbCurl = 0f;
        indexCurl = 0f;
        middleCurl = 0f;
        ringCurl = 0f;
        pinkyCurl = 0f;
    }

    private void CacheStartRotation(FingerChain finger)
    {
        if (finger == null) return;

        if (finger.bone1 != null) finger.bone1Start = finger.bone1.localRotation;
        if (finger.bone2 != null) finger.bone2Start = finger.bone2.localRotation;
        if (finger.bone3 != null) finger.bone3Start = finger.bone3.localRotation;
    }

    private void ResetFinger(FingerChain finger)
    {
        if (finger == null) return;

        if (finger.bone1 != null) finger.bone1.localRotation = finger.bone1Start;
        if (finger.bone2 != null) finger.bone2.localRotation = finger.bone2Start;
        if (finger.bone3 != null) finger.bone3.localRotation = finger.bone3Start;
    }

        public void SetFingerCurls(float thumbCurl, float indexCurl, float middleCurl, float ringCurl, float pinkyCurl)
    {
        ApplyFingerCurl(thumb, Mathf.Clamp(thumbCurl, -1f, 1f));
        ApplyFingerCurl(index, Mathf.Clamp(indexCurl, -1f, 1f));
        ApplyFingerCurl(middle, Mathf.Clamp(middleCurl, -1f, 1f));
        ApplyFingerCurl(ring, Mathf.Clamp(ringCurl, -1f, 1f));
        ApplyFingerCurl(pinky, Mathf.Clamp(pinkyCurl, -1f, 1f));
    }
    private void ApplyFingerCurl(FingerChain finger, float curl)
    {
        if (finger == null) return;

        if (finger.bone1 != null)
            finger.bone1.localRotation = finger.bone1Start *
                                        Quaternion.AngleAxis(finger.angle1 * curl, finger.axis1.normalized);

        if (finger.bone2 != null)
            finger.bone2.localRotation = finger.bone2Start *
                                        Quaternion.AngleAxis(finger.angle2 * curl, finger.axis2.normalized);

        if (finger.bone3 != null)
            finger.bone3.localRotation = finger.bone3Start *
                                        Quaternion.AngleAxis(finger.angle3 * curl, finger.axis3.normalized);
    }
}