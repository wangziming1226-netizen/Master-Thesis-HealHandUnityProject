using UnityEngine;
using UnityEngine.UI;
using Thesis.Core;

namespace Thesis.UI
{
    public class HandOverlayController : MonoBehaviour
    {
        [Header("Optional Runtime Sources")]
        [SerializeField] private ThesisGestureInput gestureInput;
        [SerializeField] private HandGestureRecognizer recognizer;

        [Header("UI")]
        [SerializeField] private RectTransform overlayImageRect;
        [SerializeField] private RectTransform referenceRect;
        [SerializeField] private Image overlayImage;

        [Header("Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite fistSprite;
        [SerializeField] private Sprite okSprite;
        [SerializeField] private Sprite thumbsUpSprite;
        [SerializeField] private Sprite lostSprite;

        [Header("Position Mapping")]
        [SerializeField] private bool invertPositionX = false;
        [SerializeField] private bool invertPositionY = false;
        [SerializeField] private Vector2 positionOffset = Vector2.zero;
        [SerializeField] private float followSpeed = 8f;

        [Header("Rotation")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private bool invertRotation = true;
        [SerializeField] private float rotationLerpSpeed = 10f;
        [SerializeField] private float rotationOffset = 0f;
        [SerializeField] private float maxRotation = 45f;

        [Header("Visibility")]
        [SerializeField] private bool hideWhenLost = false;

        private Vector2 currentAnchoredPos;
        private bool hasInitialPosition;

        private float currentRotationZ;
        private bool hasInitialRotation;

        private void Start()
        {
            TryResolveSources();
        }

        private void Update()
        {
            TryResolveSources();

            if (gestureInput == null || recognizer == null || overlayImageRect == null ||
                referenceRect == null || overlayImage == null)
                return;

            bool tracking = gestureInput.IsTracking;
            UpdateSprite(tracking);

            if (!tracking)
            {
                overlayImageRect.gameObject.SetActive(!hideWhenLost);
                return;
            }

            overlayImageRect.gameObject.SetActive(true);

            Vector2[] points = recognizer.LastPoints;
            if (points == null || points.Length < 18)
                return;

            UpdatePosition(points);
            UpdateRotation(points);
        }

        private void UpdatePosition(Vector2[] points)
        {
            Vector2 palmCenter = GetPalmCenter(points);
            Vector2 targetPosition = NormalizedToAnchored(referenceRect, palmCenter) + positionOffset;

            if (!hasInitialPosition)
            {
                currentAnchoredPos = targetPosition;
                hasInitialPosition = true;
            }
            else
            {
                currentAnchoredPos = Vector2.Lerp(
                    currentAnchoredPos,
                    targetPosition,
                    Time.deltaTime * followSpeed
                );
            }

            overlayImageRect.anchoredPosition = currentAnchoredPos;
        }

        private void UpdateRotation(Vector2[] points)
        {
            if (!enableRotation)
            {
                overlayImageRect.localEulerAngles = Vector3.zero;
                return;
            }

            float targetAngle = GetHandAngle(points) + rotationOffset;
            targetAngle = Mathf.Clamp(targetAngle, -maxRotation, maxRotation);

            if (!hasInitialRotation)
            {
                currentRotationZ = targetAngle;
                hasInitialRotation = true;
            }
            else
            {
                currentRotationZ = Mathf.Lerp(
                    currentRotationZ,
                    targetAngle,
                    Time.deltaTime * rotationLerpSpeed
                );
            }

            overlayImageRect.localEulerAngles = new Vector3(0f, 0f, currentRotationZ);
        }

        private void TryResolveSources()
        {
            if (gestureInput == null)
                gestureInput = FindFirstObjectByType<ThesisGestureInput>();

            if (recognizer == null)
                recognizer = FindFirstObjectByType<HandGestureRecognizer>();
        }

        private void UpdateSprite(bool tracking)
        {
            if (!tracking)
            {
                overlayImage.sprite = lostSprite != null ? lostSprite : idleSprite;
                return;
            }

            switch (gestureInput.currentGesture)
            {
                case "open":
                case "open_hand":
                    if (openSprite != null)
                        overlayImage.sprite = openSprite;
                    break;

                case "fist":
                    if (fistSprite != null)
                        overlayImage.sprite = fistSprite;
                    break;

                case "ok":
                    if (okSprite != null)
                        overlayImage.sprite = okSprite;
                    break;

                case "thumbs_up":
                case "thumbsup":
                case "thumbs up":
                    if (thumbsUpSprite != null)
                        overlayImage.sprite = thumbsUpSprite;
                    break;

                default:
                    if (idleSprite != null)
                        overlayImage.sprite = idleSprite;
                    break;
            }
        }

        private Vector2 GetPalmCenter(Vector2[] points)
        {
            return (points[0] + points[5] + points[9] + points[17]) / 4f;
        }

        private float GetHandAngle(Vector2[] points)
        {
            Vector2 direction = points[5] - points[17];
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            return invertRotation ? -angle : angle;
        }

        private Vector2 NormalizedToAnchored(RectTransform rect, Vector2 point)
        {
            float x = (point.x - 0.5f) * rect.rect.width;
            float y = (point.y - 0.5f) * rect.rect.height;

            if (invertPositionX)
                x = -x;

            if (invertPositionY)
                y = -y;

            return new Vector2(x, y);
        }
    }
}
