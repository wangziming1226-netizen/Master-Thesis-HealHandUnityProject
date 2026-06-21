using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Thesis.Core;

namespace Thesis.UI
{
    public class AvatarStateController : MonoBehaviour
    {
        [Header("Optional manual sources")]
        [SerializeField] private ThesisGestureInput gestureInput;
        [SerializeField] private GestureJudge gestureJudge;

        [Header("UI")]
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image handImage;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject lostTrackingHint;
        [SerializeField] private GameObject successGlow;
        [SerializeField] private Image progressFill;

        [Header("Hand Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite openHandSprite;
        [SerializeField] private Sprite fistSprite;
        [SerializeField] private Sprite okSprite;
        [SerializeField] private Sprite lostTrackingSprite;

        [SerializeField] private float successGlowDuration = 0.8f;

        private float successGlowTimer = 0f;
        private bool judgeBound = false;

        private void Start()
        {
            TryResolveSources();
            BindJudgeEvents();
            RefreshAvatarState();
        }

        private void Update()
        {
            if (gestureInput == null || gestureJudge == null)
            {
                TryResolveSources();
                BindJudgeEvents();
            }

            RefreshAvatarState();

            if (successGlow != null && successGlow.activeSelf)
            {
                successGlowTimer -= Time.deltaTime;
                if (successGlowTimer <= 0f)
                    successGlow.SetActive(false);
            }
        }

        private void OnDisable()
        {
            UnbindJudgeEvents();
        }

        private void TryResolveSources()
        {
            if (gestureInput == null)
                gestureInput = FindFirstObjectByType<ThesisGestureInput>();

            if (gestureJudge == null)
                gestureJudge = FindFirstObjectByType<GestureJudge>();
        }

        private void BindJudgeEvents()
        {
            if (judgeBound || gestureJudge == null) return;

            gestureJudge.onProgress.AddListener(OnProgress);
            gestureJudge.onSuccess.AddListener(OnSuccess);
            judgeBound = true;
        }

        private void UnbindJudgeEvents()
        {
            if (!judgeBound || gestureJudge == null) return;

            gestureJudge.onProgress.RemoveListener(OnProgress);
            gestureJudge.onSuccess.RemoveListener(OnSuccess);
            judgeBound = false;
        }

        private void RefreshAvatarState()
        {
            if (gestureInput == null) return;

            bool tracking = gestureInput.IsTracking;
            string gesture = gestureInput.currentGesture;

            if (!tracking)
            {
                SetState("Tracking Lost", lostTrackingSprite != null ? lostTrackingSprite : idleSprite, true);
                return;
            }

            switch (gesture)
            {
                case "open_hand":
                case "open":
                    SetState("Open Hand", openHandSprite, false);
                    break;

                case "fist":
                    SetState("Fist", fistSprite, false);
                    break;

                case "ok":
                    SetState("OK", okSprite, false);
                    break;

                default:
                    SetState("Ready", idleSprite, false);
                    break;
            }
        }

        private void SetState(string text, Sprite sprite, bool lostTracking)
        {
            if (statusText != null) statusText.text = text;
            if (handImage != null && sprite != null) handImage.sprite = sprite;
            if (lostTrackingHint != null) lostTrackingHint.SetActive(lostTracking);
        }

        private void OnProgress(float value)
        {
            if (progressFill != null)
                progressFill.fillAmount = Mathf.Clamp01(value);
        }

        private void OnSuccess()
        {
            if (successGlow != null)
            {
                successGlow.SetActive(true);
                successGlowTimer = successGlowDuration;
            }

            if (statusText != null)
                statusText.text = "Success";
        }
    }
}