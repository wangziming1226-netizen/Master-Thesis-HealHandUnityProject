using System;
using UnityEngine;

public class ContinuousHandRigBridge : MonoBehaviour
{
    [Serializable]
    public class FingerCalibration
    {
        [Header("Raw Curl Mapping")]
        public float offset = 0f;
        public float gain = 1f;

        [Header("Angle Range")]
        public float straightAngle = 175f;
        public float curledAngle = 65f;
    }

    [Serializable]
    public class FingerPoseRange
    {
        [Header("Visual Range")]
        [Range(-1f, 1f)] public float openCurl = 0f;
        [Range(-1f, 1f)] public float fistCurl = 1f;
    }

        [Serializable]
    public class FingerRawRange
    {
        public float openRaw = 0f;
        public float fistRaw = 1f;
    }

    [Header("References")]
    public HandRigController handRig;
    public HandLandmarkCache landmarkCache;

    [Header("Auto Find")]
    public bool autoFindHandRig = true;
    public bool autoFindLandmarkCache = true;
    public float refindInterval = 1.0f;

    [Header("Smoothing")]
    [Range(1f, 30f)] public float smoothSpeed = 10f;

    [Header("Tracking")]
    public bool resetToOpenWhenLost = true;

    [Header("Per-Finger Raw Calibration")]


    [Header("Per-Finger Observed Raw Range")]
    public FingerRawRange thumbRawRange = new FingerRawRange
    {
        openRaw = 0.065f,
        fistRaw = 0.187f
    };

    public FingerRawRange indexRawRange = new FingerRawRange
    {
        openRaw = 0.000f,
        fistRaw = 0.574f
    };

    public FingerRawRange middleRawRange = new FingerRawRange
    {
        openRaw = 0.006f,
        fistRaw = 0.493f
    };

    public FingerRawRange ringRawRange = new FingerRawRange
    {
        openRaw = 0.027f,
        fistRaw = 0.443f
    };

    public FingerRawRange pinkyRawRange = new FingerRawRange
    {
        openRaw = 0.018f,
        fistRaw = 0.395f
    };
    public FingerCalibration thumbCal = new FingerCalibration
    {
        offset = 0.05f,
        gain = 0.7f,
        straightAngle = 170f,
        curledAngle = 75f
    };

    public FingerCalibration indexCal = new FingerCalibration
    {
        offset = 0f,
        gain = 1.0f,
        straightAngle = 175f,
        curledAngle = 65f
    };

    public FingerCalibration middleCal = new FingerCalibration
    {
        offset = 0f,
        gain = 0.85f,
        straightAngle = 175f,
        curledAngle = 65f
    };

    public FingerCalibration ringCal = new FingerCalibration
    {
        offset = 0f,
        gain = 0.75f,
        straightAngle = 175f,
        curledAngle = 65f
    };

    public FingerCalibration pinkyCal = new FingerCalibration
    {
        offset = 0f,
        gain = 0.65f,
        straightAngle = 175f,
        curledAngle = 65f
    };

    [Header("Per-Finger Visual Pose Range")]
    public FingerPoseRange thumbPose = new FingerPoseRange
    {
        openCurl = 0.05f,
        fistCurl = 0.35f
    };

    public FingerPoseRange indexPose = new FingerPoseRange
    {
        openCurl = 0.00f,
        fistCurl = 1.00f
    };

    public FingerPoseRange middlePose = new FingerPoseRange
    {
        openCurl = 0.00f,
        fistCurl = 0.80f
    };

    public FingerPoseRange ringPose = new FingerPoseRange
    {
        openCurl = 0.00f,
        fistCurl = 0.75f
    };

    public FingerPoseRange pinkyPose = new FingerPoseRange
    {
        openCurl = 0.00f,
        fistCurl = 0.70f
    };

    [Header("Debug Runtime")]
    [Range(0f, 1f)] public float rawThumb;
    [Range(0f, 1f)] public float rawIndex;
    [Range(0f, 1f)] public float rawMiddle;
    [Range(0f, 1f)] public float rawRing;
    [Range(0f, 1f)] public float rawPinky;

    [Range(-1f, 1f)] public float thumbCurl;
    [Range(-1f, 1f)] public float indexCurl;
    [Range(-1f, 1f)] public float middleCurl;
    [Range(-1f, 1f)] public float ringCurl;
    [Range(-1f, 1f)] public float pinkyCurl;

    private float nextRefindTime = 0f;

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
        if ((handRig == null || landmarkCache == null) && Time.time >= nextRefindTime)
        {
            TryResolveReferences();
            nextRefindTime = Time.time + refindInterval;
        }

        if (handRig == null)
            return;

        if (landmarkCache == null || !landmarkCache.HasValidData)
        {
            if (resetToOpenWhenLost)
            {
                SmoothToTargets(
                    thumbPose.openCurl,
                    indexPose.openCurl,
                    middlePose.openCurl,
                    ringPose.openCurl,
                    pinkyPose.openCurl
                );
            }

            ApplyToRig();
            return;
        }

        Vector3[] pts = landmarkCache.GetLandmarksCopy();

        // 0 wrist
        // thumb: 1 2 3 4
        // index: 5 6 7 8
        // middle: 9 10 11 12
        // ring: 13 14 15 16
        // pinky: 17 18 19 20

        rawThumb  = ComputeFingerCurl(pts[0], pts[1],  pts[2],  pts[3],  pts[4],  thumbCal);
        rawIndex  = ComputeFingerCurl(pts[0], pts[5],  pts[6],  pts[7],  pts[8],  indexCal);
        rawMiddle = ComputeFingerCurl(pts[0], pts[9],  pts[10], pts[11], pts[12], middleCal);
        rawRing   = ComputeFingerCurl(pts[0], pts[13], pts[14], pts[15], pts[16], ringCal);
        rawPinky  = ComputeFingerCurl(pts[0], pts[17], pts[18], pts[19], pts[20], pinkyCal);

        float normThumb  = NormalizeByObservedRange(rawThumb,  thumbRawRange);
        float normIndex  = NormalizeByObservedRange(rawIndex,  indexRawRange);
        float normMiddle = NormalizeByObservedRange(rawMiddle, middleRawRange);
        float normRing   = NormalizeByObservedRange(rawRing,   ringRawRange);
        float normPinky  = NormalizeByObservedRange(rawPinky,  pinkyRawRange);

        float targetThumb  = Mathf.Lerp(thumbPose.openCurl,  thumbPose.fistCurl,  normThumb);
        float targetIndex  = Mathf.Lerp(indexPose.openCurl,  indexPose.fistCurl,  normIndex);
        float targetMiddle = Mathf.Lerp(middlePose.openCurl, middlePose.fistCurl, normMiddle);
        float targetRing   = Mathf.Lerp(ringPose.openCurl,   ringPose.fistCurl,   normRing);
        float targetPinky  = Mathf.Lerp(pinkyPose.openCurl,  pinkyPose.fistCurl,  normPinky);
        SmoothToTargets(targetThumb, targetIndex, targetMiddle, targetRing, targetPinky);
        ApplyToRig();
    }

    private void TryResolveReferences()
    {
        if (autoFindHandRig && handRig == null)
        {
            handRig = GetComponent<HandRigController>();

            if (handRig == null)
                handRig = GetComponentInChildren<HandRigController>(true);

            if (handRig == null)
                handRig = FindFirstObjectByType<HandRigController>();
        }

        if (autoFindLandmarkCache && landmarkCache == null)
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

    private void SmoothToTargets(float targetThumb, float targetIndex, float targetMiddle, float targetRing, float targetPinky)
    {
        thumbCurl  = Mathf.Lerp(thumbCurl,  targetThumb,  Time.deltaTime * smoothSpeed);
        indexCurl  = Mathf.Lerp(indexCurl,  targetIndex,  Time.deltaTime * smoothSpeed);
        middleCurl = Mathf.Lerp(middleCurl, targetMiddle, Time.deltaTime * smoothSpeed);
        ringCurl   = Mathf.Lerp(ringCurl,   targetRing,   Time.deltaTime * smoothSpeed);
        pinkyCurl  = Mathf.Lerp(pinkyCurl,  targetPinky,  Time.deltaTime * smoothSpeed);
    }

    private void ApplyToRig()
    {
        handRig.SetFingerCurls(thumbCurl, indexCurl, middleCurl, ringCurl, pinkyCurl);
    }

    private float ComputeFingerCurl(Vector3 wrist, Vector3 mcp, Vector3 pip, Vector3 dip, Vector3 tip, FingerCalibration cal)
    {
        float angleMcp = JointAngle(wrist, mcp, pip);
        float anglePip = JointAngle(mcp, pip, dip);
        float angleDip = JointAngle(pip, dip, tip);

        float curlMcp = AngleToCurl(angleMcp, cal.straightAngle, cal.curledAngle);
        float curlPip = AngleToCurl(anglePip, cal.straightAngle, cal.curledAngle);
        float curlDip = AngleToCurl(angleDip, cal.straightAngle, cal.curledAngle);

        float raw = curlMcp * 0.2f + curlPip * 0.5f + curlDip * 0.3f;

        float mapped = cal.offset + raw * cal.gain;
        return Mathf.Clamp01(mapped);
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
        private float NormalizeByObservedRange(float raw, FingerRawRange range)
    {
        if (Mathf.Approximately(range.openRaw, range.fistRaw))
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(range.openRaw, range.fistRaw, raw));
    }
}