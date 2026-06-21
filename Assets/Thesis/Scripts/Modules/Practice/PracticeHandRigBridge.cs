using System;
using UnityEngine;
using Thesis.Core;

public class PracticeHandRigBridge : MonoBehaviour
{
    [Serializable]
    public class HandPose
    {
        [Range(0f, 1f)] public float thumb = 0f;
        [Range(0f, 1f)] public float index = 0f;
        [Range(0f, 1f)] public float middle = 0f;
        [Range(0f, 1f)] public float ring = 0f;
        [Range(0f, 1f)] public float pinky = 0f;
    }

    [Header("References")]
    public HandRigController handRig;
    public ThesisGestureInput gestureInput;

    [Header("Auto Find")]
    public bool autoFindHandRig = true;
    public bool autoFindGestureInput = true;
    public float refindInterval = 1.0f;

    [Header("Smoothing")]
    [Range(1f, 20f)] public float smoothSpeed = 8f;

    [Header("Tracking")]
    public bool openOnTrackingLost = true;

    [Header("Pose Presets")]
    public HandPose defaultPose = new HandPose
    {
        thumb = 0.10f,
        index = 0.10f,
        middle = 0.10f,
        ring = 0.10f,
        pinky = 0.10f
    };

    public HandPose lostTrackingPose = new HandPose
    {
        thumb = 0.00f,
        index = 0.00f,
        middle = 0.00f,
        ring = 0.00f,
        pinky = 0.00f
    };

    public HandPose openPose = new HandPose
    {
        thumb = 0.00f,
        index = 0.00f,
        middle = 0.00f,
        ring = 0.00f,
        pinky = 0.00f
    };

    public HandPose fistPose = new HandPose
    {
        thumb = 0.35f,
        index = 1.00f,
        middle = 0.80f,
        ring = 0.75f,
        pinky = 0.70f
    };

    public HandPose okPose = new HandPose
    {
        thumb = 0.55f,
        index = 0.55f,
        middle = 0.18f,
        ring = 0.15f,
        pinky = 0.12f
    };

    private float thumbCurl;
    private float indexCurl;
    private float middleCurl;
    private float ringCurl;
    private float pinkyCurl;

    private float nextRefindTime = 0f;

    private void Awake()
    {
        TryResolveReferences(forceLog: false);
    }

    private void OnEnable()
    {
        TryResolveReferences(forceLog: false);
    }

    private void Update()
    {
        if ((handRig == null || gestureInput == null) && Time.time >= nextRefindTime)
        {
            TryResolveReferences(forceLog: false);
            nextRefindTime = Time.time + refindInterval;
        }

        HandPose targetPose = ResolveTargetPose();

        thumbCurl  = Mathf.Lerp(thumbCurl,  targetPose.thumb,  Time.deltaTime * smoothSpeed);
        indexCurl  = Mathf.Lerp(indexCurl,  targetPose.index,  Time.deltaTime * smoothSpeed);
        middleCurl = Mathf.Lerp(middleCurl, targetPose.middle, Time.deltaTime * smoothSpeed);
        ringCurl   = Mathf.Lerp(ringCurl,   targetPose.ring,   Time.deltaTime * smoothSpeed);
        pinkyCurl  = Mathf.Lerp(pinkyCurl,  targetPose.pinky,  Time.deltaTime * smoothSpeed);

        if (handRig != null)
        {
            handRig.SetFingerCurls(thumbCurl, indexCurl, middleCurl, ringCurl, pinkyCurl);
        }
    }

    private void TryResolveReferences(bool forceLog)
    {
        if (autoFindHandRig && handRig == null)
        {
            handRig = GetComponent<HandRigController>();

            if (handRig == null)
                handRig = GetComponentInChildren<HandRigController>(true);

            if (handRig == null)
                handRig = FindFirstObjectByType<HandRigController>();
        }

        if (autoFindGestureInput && gestureInput == null)
        {
            gestureInput = FindFirstObjectByType<ThesisGestureInput>();

            if (gestureInput == null)
            {
                ThesisGestureInput[] all = Resources.FindObjectsOfTypeAll<ThesisGestureInput>();
                foreach (var item in all)
                {
                    if (item == null) continue;
                    if (!item.gameObject.scene.isLoaded) continue;
                    gestureInput = item;
                    break;
                }
            }
        }

        if (forceLog)
        {
            Debug.Log(
                $"[PracticeHandRigBridge] handRig={(handRig != null ? handRig.name : "null")}, " +
                $"gestureInput={(gestureInput != null ? gestureInput.name : "null")}",
                this
            );
        }
    }

    private HandPose ResolveTargetPose()
    {
        if (gestureInput == null)
            return defaultPose;

        if (!gestureInput.IsTracking)
            return openOnTrackingLost ? lostTrackingPose : defaultPose;

        switch (gestureInput.currentGesture)
        {
            case "open_hand":
                return openPose;

            case "fist":
                return fistPose;

            case "ok":
                return okPose;

            default:
                return defaultPose;
        }
    }

    [ContextMenu("Log Current References")]
    private void LogCurrentReferences()
    {
        TryResolveReferences(forceLog: true);
    }
}