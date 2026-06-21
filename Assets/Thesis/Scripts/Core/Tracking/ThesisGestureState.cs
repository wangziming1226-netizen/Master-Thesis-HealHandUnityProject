using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Thesis.Core
{
    public class ThesisGestureState : MonoBehaviour
    {
        public static ThesisGestureState Instance { get; private set; }

        [Header("Runtime State (read-only)")]
        [SerializeField] private string currentGesture = "none";
        [SerializeField] private float lastSeenTime = -999f;

        [Header("Behaviour")]
        [Tooltip("If no update arrives within this seconds, gesture becomes stale and resets to none.")]
        public float staleTimeout = 0.80f;

        [Tooltip("Optional: require the same gesture to be stable for this long before applying.")]
        public float minStableSeconds = 0.0f;

        [Header("Events")]
        public UnityEvent<string> OnGestureChanged;
        public UnityEvent OnGestureCleared;

        private string _pendingGesture = "none";
        private float _pendingSince = -999f;

        private static readonly HashSet<string> AllowedGestures = new HashSet<string>
        {
            "none","open_hand","fist","ok",
            "pinch","point","thumb_up","v_sign","three",
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[ThesisGestureState] Duplicate instance detected, destroying THIS COMPONENT only. " +
                    $"existing(obj={Instance.gameObject.name}, scene={Instance.gameObject.scene.name}) " +
                    $"new(obj={gameObject.name}, scene={gameObject.scene.name})",
                    this
                );

                try
                {
                    OnGestureChanged?.RemoveAllListeners();
                    OnGestureCleared?.RemoveAllListeners();
                }
                catch { /* ignore */ }

                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log($"[ThesisGestureState] Instance set: obj={gameObject.name} scene={gameObject.scene.name} id={GetInstanceID()}",
                this);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (currentGesture != "none" && !IsFresh(Time.time))
                ClearGesture();
        }

        public string CurrentGesture => (!IsFresh(Time.time)) ? "none" : currentGesture;
        public float LastSeenTime => lastSeenTime;

        public bool HasFreshGesture =>
            IsFresh(Time.time) && !string.IsNullOrEmpty(currentGesture) && currentGesture != "none";

        public void SetGesture(string rawGestureKey)
        {
            float now = Time.time;
            string normalized = NormalizeGestureKey(rawGestureKey);

            lastSeenTime = now;

            if (minStableSeconds <= 0f)
            {
                ApplyGestureIfChanged(normalized);
                return;
            }

            if (_pendingGesture != normalized)
            {
                _pendingGesture = normalized;
                _pendingSince = now;
                return;
            }

            if (now - _pendingSince >= minStableSeconds)
                ApplyGestureIfChanged(normalized);
        }

        public void OnOpenHand() => SetGesture("open_hand");
        public void OnFist() => SetGesture("fist");
        public void OnOk() => SetGesture("ok");

        public void ClearGesture()
        {
            if (currentGesture == "none") return;

            currentGesture = "none";
            _pendingGesture = "none";
            _pendingSince = -999f;

            OnGestureCleared?.Invoke();
            OnGestureChanged?.Invoke("none");
        }

        public bool IsGestureActive(string expectedKey)
        {
            string normalized = NormalizeGestureKey(expectedKey);
            return HasFreshGesture && currentGesture == normalized;
        }

        private bool IsFresh(float now) => (now - lastSeenTime) <= staleTimeout;

        private void ApplyGestureIfChanged(string normalized)
        {
            if (!AllowedGestures.Contains(normalized))
                normalized = "none";

            if (currentGesture == normalized)
                return;

            currentGesture = normalized;
            OnGestureChanged?.Invoke(currentGesture);
        }

        public static string NormalizeGestureKey(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "none";
            string s = raw.Trim().ToLowerInvariant();

            if (s == "open hand" || s == "openhand" || s == "open_hand" || s == "open-palm" || s == "openpalm")
                return "open_hand";

            if (s == "fist" || s == "closed_fist" || s == "closed fist" || s == "close_fist")
                return "fist";

            if (s == "ok" || s == "okay" || s == "ok_sign" || s == "ok sign")
                return "ok";

            if (s == "pinch" || s == "pinch_2" || s == "pinch2") return "pinch";
            if (s == "point" || s == "pointing" || s == "index" || s == "index_up" || s == "index up") return "point";
            if (s == "thumb_up" || s == "thumbup" || s == "thumb up" || s == "like") return "thumb_up";
            if (s == "v" || s == "v_sign" || s == "v sign" || s == "peace" || s == "peace_sign") return "v_sign";
            if (s == "three" || s == "3") return "three";

            if (s == "none" || s == "unknown" || s == "null") return "none";
            if (AllowedGestures.Contains(s)) return s;

            return "none";
        }
    }
}