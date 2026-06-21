using System;
using System.Reflection;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thesis.Modules.Story
{
    public class StoryModeController : MonoBehaviour
    {
        private enum StoryStep
        {
            OpenHand = 0,
            Fist = 1,
            OK = 2,
            Complete = 3
        }

        [Header("Gesture Source")]
        [Tooltip("Assign ThesisGestureInput here. If left empty, the component will be found automatically at runtime.")]
        public MonoBehaviour gestureSource;

        [Header("Story Image")]
        public Image storyImage;
        public CanvasGroup storyImageGroup;

        [Header("Story Sprites")]
        public Sprite gardenStep0;
        public Sprite gardenStep1;
        public Sprite gardenStep2;
        public Sprite gardenStep3;

        [Header("Texts")]
        public TMP_Text txtTitle;
        public TMP_Text txtStoryMessage;
        public TMP_Text txtTargetGesture;
        public TMP_Text txtFeedback;
        public TMP_Text txtProgress;

        [Header("Visual Effects")]
        public GameObject sparkleRoot;
        public float sparkleDuration = 0.8f;
        public float fadeDuration = 0.35f;

        [Header("Gesture Hold")]
        public float requiredHoldSeconds = 1.0f;

        [Header("Runtime")]
        public bool autoStartOnEnable = true;
        public bool storyRunning = false;
        public bool storyComplete = false;
        public int currentStepIndex = 0;
        public float holdTimer = 0f;
        public string detectedGesture = "None";
        public bool isTracking = false;
        public string currentState = "Idle";

        private bool isTransitioning = false;

        private void Awake()
        {
            AutoFindGestureSource();

            if (storyImageGroup == null && storyImage != null)
                storyImageGroup = storyImage.GetComponent<CanvasGroup>();

            if (sparkleRoot != null)
                sparkleRoot.SetActive(false);

            ResetStory(false);
        }

        private void OnEnable()
        {
            AutoFindGestureSource();

            if (autoStartOnEnable)
                StartStory();
            else
                RefreshUI();
        }

        private void Update()
        {
            if (!storyRunning || storyComplete || isTransitioning)
                return;

            UpdateGestureRuntimeState();

            if (!isTracking)
            {
                holdTimer = 0f;
                currentState = "Tracking lost";
                SetFeedback("Move your hand into view.");
                RefreshProgressText();
                return;
            }

            bool matched = IsTargetGestureMatched();

            if (matched)
            {
                holdTimer += Time.deltaTime;
                currentState = "Holding";

                SetFeedback($"Good! Hold steady... {holdTimer:0.0}/{requiredHoldSeconds:0.0}s");
                RefreshProgressText();

                if (holdTimer >= requiredHoldSeconds)
                    StartCoroutine(CompleteCurrentStep());
            }
            else
            {
                holdTimer = 0f;
                currentState = "Waiting";

                SetFeedback($"Show {GetTargetGestureLabel()} to continue the story.");
                RefreshProgressText();
            }
        }

        public void StartStory()
        {
            storyRunning = true;
            storyComplete = false;
            isTransitioning = false;

            currentStepIndex = 0;
            holdTimer = 0f;
            currentState = "Running";

            if (storyImage != null)
                storyImage.sprite = gardenStep0;

            if (storyImageGroup != null)
                storyImageGroup.alpha = 1f;

            if (sparkleRoot != null)
                sparkleRoot.SetActive(false);

            RefreshUI();
            SetFeedback("Show an Open Hand to begin the magic.");
        }

        public void ResetStory()
        {
            ResetStory(true);
        }

        private void ResetStory(bool restartImmediately)
        {
            storyRunning = restartImmediately;
            storyComplete = false;
            isTransitioning = false;

            currentStepIndex = 0;
            holdTimer = 0f;
            currentState = restartImmediately ? "Running" : "Idle";

            if (storyImage != null)
                storyImage.sprite = gardenStep0;

            if (storyImageGroup != null)
                storyImageGroup.alpha = 1f;

            if (sparkleRoot != null)
                sparkleRoot.SetActive(false);

            RefreshUI();

            if (restartImmediately)
                SetFeedback("Show an Open Hand to begin the magic.");
            else
                SetFeedback("Enter Story mode to begin.");
        }

        private IEnumerator CompleteCurrentStep()
        {
            if (isTransitioning)
                yield break;

            isTransitioning = true;
            currentState = "Step success";

            SetFeedback(GetSuccessMessage(currentStepIndex));

            if (sparkleRoot != null)
            {
                sparkleRoot.SetActive(true);
                StartCoroutine(HideSparkleAfterDelay());
            }

            yield return new WaitForSeconds(0.7f);

            int nextVisualIndex = Mathf.Clamp(currentStepIndex + 1, 0, 3);
            Sprite nextSprite = GetSpriteByIndex(nextVisualIndex);

            yield return StartCoroutine(FadeToSprite(nextSprite));

            currentStepIndex++;
            holdTimer = 0f;

            if (currentStepIndex >= 3)
            {
                storyComplete = true;
                storyRunning = false;
                currentState = "Complete";

                RefreshUI();
                SetFeedback("Story Complete! Well done!");
            }
            else
            {
                currentState = "Running";

                RefreshUI();
                SetFeedback($"Now show {GetTargetGestureLabel()}.");
            }

            isTransitioning = false;
        }

        private IEnumerator FadeToSprite(Sprite nextSprite)
        {
            if (storyImage == null)
                yield break;

            if (storyImageGroup == null)
            {
                storyImage.sprite = nextSprite;
                yield break;
            }

            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                storyImageGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }

            storyImage.sprite = nextSprite;

            t = 0f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                storyImageGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
                yield return null;
            }

            storyImageGroup.alpha = 1f;
        }

        private IEnumerator HideSparkleAfterDelay()
        {
            yield return new WaitForSeconds(sparkleDuration);

            if (sparkleRoot != null)
                sparkleRoot.SetActive(false);
        }

        private void UpdateGestureRuntimeState()
        {
            detectedGesture = ReadGestureName();
            isTracking = ReadTrackingState();
        }

        private bool IsTargetGestureMatched()
        {
            string g = detectedGesture;

            if (string.IsNullOrEmpty(g))
                return false;

            switch ((StoryStep)currentStepIndex)
            {
                case StoryStep.OpenHand:
                    return g == "OpenHand" || g == "Open" || g == "Open_Hand" || g == "open_hand";

                case StoryStep.Fist:
                    return g == "Fist" || g == "fist";

                case StoryStep.OK:
                    return g == "OK" || g == "Ok" || g == "ok";

                default:
                    return false;
            }
        }

        private string ReadGestureName()
        {
            if (gestureSource == null)
                return "None";

            Type t = gestureSource.GetType();

            FieldInfo field =
                t.GetField("currentGesture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("CurrentGesture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("gesture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null)
            {
                object value = field.GetValue(gestureSource);
                return value != null ? value.ToString() : "None";
            }

            PropertyInfo prop =
                t.GetProperty("currentGesture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("CurrentGesture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("Gesture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop != null)
            {
                object value = prop.GetValue(gestureSource);
                return value != null ? value.ToString() : "None";
            }

            return "None";
        }

        private bool ReadTrackingState()
        {
            if (gestureSource == null)
                return false;

            Type t = gestureSource.GetType();

            FieldInfo field =
                t.GetField("IsTracking", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("isTracking", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("trackingPresent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetField("hasHand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(gestureSource);

            PropertyInfo prop =
                t.GetProperty("IsTracking", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("isTracking", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("TrackingPresent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                t.GetProperty("HasHand", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(gestureSource);

            return false;
        }

        private void AutoFindGestureSource()
        {
            if (gestureSource != null)
                return;

            MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (MonoBehaviour mb in all)
            {
                if (mb == null) continue;

                if (mb.GetType().Name == "ThesisGestureInput")
                {
                    gestureSource = mb;
                    return;
                }
            }
        }

        private Sprite GetSpriteByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return gardenStep0;
                case 1:
                    return gardenStep1;
                case 2:
                    return gardenStep2;
                case 3:
                    return gardenStep3;
                default:
                    return gardenStep3;
            }
        }

        private string GetTargetGestureLabel()
        {
            switch ((StoryStep)currentStepIndex)
            {
                case StoryStep.OpenHand:
                    return "Open Hand";
                case StoryStep.Fist:
                    return "Fist";
                case StoryStep.OK:
                    return "OK";
                case StoryStep.Complete:
                    return "Complete";
                default:
                    return "-";
            }
        }

        private string GetStoryMessage()
        {
            if (storyComplete)
                return "The garden is full of colour now.";

            switch ((StoryStep)currentStepIndex)
            {
                case StoryStep.OpenHand:
                    return "A little garden is waiting for your help.\nShow an Open Hand to give it sunlight.";

                case StoryStep.Fist:
                    return "The flower has started to bloom.\nMake a Fist to collect garden energy.";

                case StoryStep.OK:
                    return "The garden is almost ready.\nMake an OK gesture to finish the magic spell.";

                default:
                    return "Story Complete!";
            }
        }

        private string GetSuccessMessage(int step)
        {
            switch (step)
            {
                case 0:
                    return "Great! The flower begins to bloom.";
                case 1:
                    return "Nice! The garden is getting stronger.";
                case 2:
                    return "Well done! The magic is complete.";
                default:
                    return "Great!";
            }
        }

        private void RefreshUI()
        {
            if (txtTitle != null)
                txtTitle.text = "Magic Garden";

            if (txtStoryMessage != null)
                txtStoryMessage.text = GetStoryMessage();

            if (txtTargetGesture != null)
            {
                if (storyComplete)
                    txtTargetGesture.text = "Target: Complete";
                else
                    txtTargetGesture.text = $"Target: {GetTargetGestureLabel()}";
            }

            RefreshProgressText();
        }

        private void RefreshProgressText()
        {
            if (txtProgress == null)
                return;

            if (storyComplete)
            {
                txtProgress.text = "Progress: ● ● ●";
                return;
            }

            string dots;

            if (currentStepIndex <= 0)
                dots = "○ ○ ○";
            else if (currentStepIndex == 1)
                dots = "● ○ ○";
            else if (currentStepIndex == 2)
                dots = "● ● ○";
            else
                dots = "● ● ●";

            txtProgress.text = $"Progress: {dots}   Hold: {holdTimer:0.0}/{requiredHoldSeconds:0.0}s";
        }

        private void SetFeedback(string message)
        {
            if (txtFeedback != null)
                txtFeedback.text = message;
        }
    }
}