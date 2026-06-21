using System;
using UnityEngine;

public class PalmAndIndexAngleRetarget : MonoBehaviour
{
    [Header("References")]
    public HandLandmarkCache landmarkCache;

    [Header("Auto Find")]
    public bool autoFindLandmarkCache = true;
    public float refindInterval = 1f;

    [Header("Palm Root")]
    public bool drivePalmRoot = true;
    public float palmSmooth = 10f;
    public bool invertPalmNormal = false;

    [Header("Index Bones")]
    public Transform indexBone1; // index_01.R.017
    public Transform indexBone2; // index_02.R.018
    public Transform indexBone3; // index_03.R.019

    [Header("Index Local Axes")]
    public Vector3 axis1 = new Vector3(0, 0, 1);
    public Vector3 axis2 = new Vector3(0, 0, 1);
    public Vector3 axis3 = new Vector3(0, 0, 1);

    [Header("Index Angle Gains")]
    public float gain1 = -0.35f;
    public float gain2 = -0.75f;
    public float gain3 = -0.50f;

    [Header("Index Open-Side Compensation")]
    public float openBoost1 = 2.0f;
    public float openBoost2 = 1.8f;
    public float openBoost3 = 1.5f;

    public float openClamp1 = 30f;
    public float openClamp2 = 60f;
    public float openClamp3 = 35f;

    [Header("Index Extra Angle Clamp")]
    public float clamp1 = 20f;
    public float clamp2 = 45f;
    public float clamp3 = 25f;

    [Header("Smoothing")]
    public float fingerSmooth = 14f;

    [Header("Calibration")]
    public bool autoCalibrateOnFirstValidFrame = true;
    public KeyCode recalibrateKey = KeyCode.C;

    [Header("Debug")]
    public bool calibrated = false;

    public float bindMcpAngle;
    public float bindPipAngle;
    public float bindDipAngle;

    public float curMcpAngle;
    public float curPipAngle;
    public float curDipAngle;

    public float delta1;
    public float delta2;
    public float delta3;

    private float nextRefindTime = 0f;

    private Quaternion bindRootLocalRotation;
    private Quaternion bindPalmSourceRotation;

    private Quaternion indexBone1Start;
    private Quaternion indexBone2Start;
    private Quaternion indexBone3Start;

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
            Calibrate(pts);
        }

        if (Input.GetKeyDown(recalibrateKey))
        {
            Calibrate(pts);
            Debug.Log("[PalmAndIndexAngleRetarget] Recalibrated.");
        }

        if (!calibrated)
            return;

        if (drivePalmRoot)
        {
            ApplyPalm(pts);
        }

        ApplyIndexByAngles(pts);
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

    private void Calibrate(Vector3[] pts)
    {
        bindRootLocalRotation = transform.localRotation;
        bindPalmSourceRotation = ComputePalmSourceRotation(pts);

        if (indexBone1 != null) indexBone1Start = indexBone1.localRotation;
        if (indexBone2 != null) indexBone2Start = indexBone2.localRotation;
        if (indexBone3 != null) indexBone3Start = indexBone3.localRotation;

        bindMcpAngle = JointAngle(pts[0], pts[5], pts[6]); // wrist-5-6
        bindPipAngle = JointAngle(pts[5], pts[6], pts[7]); // 5-6-7
        bindDipAngle = JointAngle(pts[6], pts[7], pts[8]); // 6-7-8

        calibrated = true;
    }
    private float ComputeFingerDelta(float rawDelta, float gain, float closeClamp, float openBoost, float openClamp)
    {
        // rawDelta >= 0: the finger is more flexed than in the calibrated pose.
        // rawDelta < 0: the finger is more extended than in the calibrated pose.
        if (rawDelta >= 0f)
        {
            return Mathf.Clamp(rawDelta * gain, -closeClamp, closeClamp);
        }
        else
        {
            return Mathf.Clamp(rawDelta * gain * openBoost, -openClamp, openClamp);
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

    private void ApplyIndexByAngles(Vector3[] pts)
    {
        curMcpAngle = JointAngle(pts[0], pts[5], pts[6]);
        curPipAngle = JointAngle(pts[5], pts[6], pts[7]);
        curDipAngle = JointAngle(pts[6], pts[7], pts[8]);

        // As the hand closes, the joint angles generally become smaller.
        // bind - current is therefore positive; with a negative gain, it produces a negative angle that curls the finger inward.
        float raw1 = bindMcpAngle - curMcpAngle;
        float raw2 = bindPipAngle - curPipAngle;
        float raw3 = bindDipAngle - curDipAngle;

        delta1 = ComputeFingerDelta(raw1, gain1, clamp1, openBoost1, openClamp1);
        delta2 = ComputeFingerDelta(raw2, gain2, clamp2, openBoost2, openClamp2);
        delta3 = ComputeFingerDelta(raw3, gain3, clamp3, openBoost3, openClamp3);

        if (indexBone1 != null)
        {
            Quaternion target = indexBone1Start * Quaternion.AngleAxis(delta1, axis1.normalized);
            indexBone1.localRotation = Quaternion.Slerp(indexBone1.localRotation, target, Time.deltaTime * fingerSmooth);
        }

        if (indexBone2 != null)
        {
            Quaternion target = indexBone2Start * Quaternion.AngleAxis(delta2, axis2.normalized);
            indexBone2.localRotation = Quaternion.Slerp(indexBone2.localRotation, target, Time.deltaTime * fingerSmooth);
        }

        if (indexBone3 != null)
        {
            Quaternion target = indexBone3Start * Quaternion.AngleAxis(delta3, axis3.normalized);
            indexBone3.localRotation = Quaternion.Slerp(indexBone3.localRotation, target, Time.deltaTime * fingerSmooth);
        }
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
        if (invertPalmNormal) normal = -normal;

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
}
