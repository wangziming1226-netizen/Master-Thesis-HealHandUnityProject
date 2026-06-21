using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public class HandResultBinder : MonoBehaviour
{
    [Header("Source (Runner on Solution)")]
    public HandLandmarkerRunner runner;

    [Header("Target (Script on GestureRecognizer)")]
    public HandGestureRecognizer recognizer;

    [Header("Optional Continuous Rig Target")]
    public HandLandmarkCache landmarkCache;

    // Used for main-thread dispatch.
    private readonly object _lockObj = new object();

    private Vector2[] _pendingPts2D;   // Used by the recogniser.
    private Vector3[] _pendingPts3D;   // Used for continuous rig driving.
    private bool _hasPending;

    void OnEnable()
    {
        if (!runner) runner = FindFirstObjectByType<HandLandmarkerRunner>(FindObjectsInactive.Include);
        if (!recognizer) recognizer = FindFirstObjectByType<HandGestureRecognizer>(FindObjectsInactive.Include);
        if (!landmarkCache) landmarkCache = FindFirstObjectByType<HandLandmarkCache>(FindObjectsInactive.Include);

        if (runner != null) runner.OnResults += HandleResults;
        else Debug.LogWarning("[Binder] HandLandmarkerRunner was not found.");
    }

    void OnDisable()
    {
        if (runner != null) runner.OnResults -= HandleResults;
    }

    // This may run on a worker thread. Do not access Unity UI or GameObject state here.
    private void HandleResults(HandLandmarkerResult result)
    {
        var pts2D = BuildPoints2D(result);   // Used by the recogniser.
        var pts3D = BuildPoints3D(result);   // Used for continuous rig driving.

        lock (_lockObj)
        {
            _pendingPts2D = pts2D;
            _pendingPts3D = pts3D;
            _hasPending = true;
        }
    }

    // Feed the recogniser and landmark cache on the main thread.
    void Update()
    {
        if (!_hasPending) return;

        Vector2[] pts2D = null;
        Vector3[] pts3D = null;

        lock (_lockObj)
        {
            if (_hasPending)
            {
                pts2D = _pendingPts2D;
                pts3D = _pendingPts3D;
                _pendingPts2D = null;
                _pendingPts3D = null;
                _hasPending = false;
            }
        }

        if (recognizer != null)
        {
            recognizer.Feed(pts2D);
        }

        if (landmarkCache != null)
        {
            if (pts3D != null && pts3D.Length >= 21)
                landmarkCache.SetLandmarks(pts3D, true);
            else
                landmarkCache.MarkTrackingLost();
        }
    }

    // -----------------------------
    // 2D points: retained for the existing recogniser.
    // -----------------------------
    private Vector2[] BuildPoints2D(HandLandmarkerResult result)
    {
        if (ReferenceEquals(result, null)) return null;

        var allHands = TryGetLandmarks(result, out string debugNote);
        if (allHands == null || allHands.Count == 0 || allHands[0] == null || allHands[0].Count == 0)
        {
            if (!string.IsNullOrEmpty(debugNote))
                Debug.Log($"[Binder] No landmarks received. {debugNote}");
            return null;
        }

        var first = allHands[0];
        int n = Math.Min(21, first.Count);
        var pts = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            var lm = first[i];
            if (TryGetXYZ(lm, out float x, out float y, out float z))
                pts[i] = new Vector2(x, y);
            else
                pts[i] = Vector2.zero;
        }
        return pts;
    }

    // -----------------------------
    // 3D points: used for the continuous rig.
    // The coordinates remain normalised and are not converted to world space.
    // x: 0~1, y: 0~1, z: relative depth
    // -----------------------------
    private Vector3[] BuildPoints3D(HandLandmarkerResult result)
    {
        if (ReferenceEquals(result, null)) return null;

        var allHands = TryGetLandmarks(result, out _);
        if (allHands == null || allHands.Count == 0 || allHands[0] == null || allHands[0].Count == 0)
            return null;

        var first = allHands[0];
        int n = Math.Min(21, first.Count);
        var pts = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            var lm = first[i];
            if (TryGetXYZ(lm, out float x, out float y, out float z))
            {
                // Centre x, flip y upward, and invert z for a more intuitive forward/inward direction.
                pts[i] = new Vector3(
                    x - 0.5f,
                    -(y - 0.5f),
                    -z
                );
            }
            else
            {
                pts[i] = Vector3.zero;
            }
        }

        return pts;
    }

    // Supports different result versions and attempts to restore the data as IList<IList<NormalizedLandmark>>.
    private IList<IList<NormalizedLandmark>> TryGetLandmarks(HandLandmarkerResult result, out string debugNote)
    {
        debugNote = null;
        object value = null;
        var t = result.GetType();

        string[] names = { "Landmarks", "landmarks", "HandLandmarks", "handLandmarks" };
        foreach (var name in names)
        {
            var p = t.GetProperty(name);
            if (p != null) { value = p.GetValue(result); break; }
            var f = t.GetField(name);
            if (f != null) { value = f.GetValue(result); break; }
        }

        if (value == null)
        {
            var props = t.GetProperties().Select(p => p.Name);
            var fields = t.GetFields().Select(f => f.Name);
            debugNote = $"Available properties: [{string.Join(", ", props)}]; available fields: [{string.Join(", ", fields)}]";

            foreach (var p in t.GetProperties())
                if (TryUnwrap(p.GetValue(result), out var list)) return list;
            foreach (var f in t.GetFields())
                if (TryUnwrap(f.GetValue(result), out var list)) return list;

            return null;
        }

        if (TryUnwrap(value, out var unwrapped)) return unwrapped;

        debugNote = $"The received {value.GetType().Name} is not a landmark list.";
        return null;
    }

    private bool TryUnwrap(object obj, out IList<IList<NormalizedLandmark>> result)
    {
        result = null;
        if (obj == null) return false;

        if (obj is IList<IList<NormalizedLandmark>> direct)
        {
            result = direct;
            return true;
        }

        if (obj is System.Collections.IList outer && outer.Count > 0)
        {
            var list = new List<IList<NormalizedLandmark>>();
            foreach (var item in outer)
            {
                if (item == null) continue;
                var it = item.GetType();

                var p = it.GetProperty("Landmark") ?? it.GetProperty("Landmarks") ??
                        it.GetProperty("landmark") ?? it.GetProperty("landmarks");
                var f = it.GetField("Landmark") ?? it.GetField("Landmarks") ??
                        it.GetField("landmark") ?? it.GetField("landmarks");

                object inner = null;
                if (p != null) inner = p.GetValue(item);
                else if (f != null) inner = f.GetValue(item);

                if (inner is IList<NormalizedLandmark> lmList)
                    list.Add(lmList);
                else if (inner is System.Collections.IList asIList && asIList.Count > 0 && asIList[0] is NormalizedLandmark)
                    list.Add(asIList.Cast<NormalizedLandmark>().ToList());
            }
            if (list.Count > 0)
            {
                result = list;
                return true;
            }
        }

        return false;
    }

    private bool TryGetXYZ(NormalizedLandmark lm, out float x, out float y, out float z)
    {
        x = y = z = 0f;
        if (lm == null) return false;

        var t = lm.GetType();

        var px = t.GetProperty("X") ?? t.GetProperty("x");
        var py = t.GetProperty("Y") ?? t.GetProperty("y");
        var pz = t.GetProperty("Z") ?? t.GetProperty("z");

        if (px != null && py != null)
        {
            x = Convert.ToSingle(px.GetValue(lm));
            y = Convert.ToSingle(py.GetValue(lm));
            if (pz != null) z = Convert.ToSingle(pz.GetValue(lm));
            return true;
        }

        var fx = t.GetField("X") ?? t.GetField("x");
        var fy = t.GetField("Y") ?? t.GetField("y");
        var fz = t.GetField("Z") ?? t.GetField("z");

        if (fx != null && fy != null)
        {
            x = Convert.ToSingle(fx.GetValue(lm));
            y = Convert.ToSingle(fy.GetValue(lm));
            if (fz != null) z = Convert.ToSingle(fz.GetValue(lm));
            return true;
        }

        return false;
    }
}
