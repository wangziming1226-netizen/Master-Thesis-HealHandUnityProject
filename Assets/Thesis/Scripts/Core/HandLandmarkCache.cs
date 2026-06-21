using System.Collections.Generic;
using UnityEngine;

public class HandLandmarkCache : MonoBehaviour
{
    [Header("Runtime")]
    public bool isTracking = false;
    public float lastUpdateTime = -999f;

    [Header("Settings")]
    public float staleAfterSeconds = 0.2f;

    [SerializeField]
    private Vector3[] landmarks = new Vector3[21];

    public bool HasValidData
    {
        get
        {
            return isTracking &&
                   landmarks != null &&
                   landmarks.Length == 21 &&
                   Time.time - lastUpdateTime <= staleAfterSeconds;
        }
    }

    public Vector3[] GetLandmarksCopy()
    {
        Vector3[] copy = new Vector3[21];
        for (int i = 0; i < 21; i++)
            copy[i] = landmarks[i];
        return copy;
    }

    public Vector3 GetLandmark(int index)
    {
        if (landmarks == null || index < 0 || index >= landmarks.Length)
            return Vector3.zero;
        return landmarks[index];
    }

    public void SetLandmarks(Vector3[] pts, bool tracking = true)
    {
        if (pts == null || pts.Length < 21) return;

        EnsureArray();
        for (int i = 0; i < 21; i++)
            landmarks[i] = pts[i];

        isTracking = tracking;
        lastUpdateTime = Time.time;
    }

    public void SetLandmarks(IList<Vector3> pts, bool tracking = true)
    {
        if (pts == null || pts.Count < 21) return;

        EnsureArray();
        for (int i = 0; i < 21; i++)
            landmarks[i] = pts[i];

        isTracking = tracking;
        lastUpdateTime = Time.time;
    }

    public void MarkTrackingLost()
    {
        isTracking = false;
    }

    private void EnsureArray()
    {
        if (landmarks == null || landmarks.Length != 21)
            landmarks = new Vector3[21];
    }
}