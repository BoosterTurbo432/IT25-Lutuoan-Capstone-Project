using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    /// <summary>
    /// Resolution-safe UI sprite-sheet frame animator for Image components.
    /// Supports looping animations (flames, prep chopping/mixing), one-shot VFX bursts
    /// (fire extinguisher foam spray), and progress-driven frame scrubbing (drink pouring).
    /// </summary>
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public class UISpriteSheetAnimator : MonoBehaviour
    {
        [Header("Animation Frames")]
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 8f;
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool useUnscaledTime;

        private Image targetImage;
        private RectTransform rectTransform;
        private Coroutine oneShotCoroutine;
        private float frameTimer;
        private int currentFrame;
        private bool isPlaying;
        private Vector3 baseScale = Vector3.one;

        public Sprite[] Frames => frames;
        public bool IsPlaying => isPlaying || oneShotCoroutine != null;
        public int CurrentFrameIndex => currentFrame;

        private void Awake()
        {
            CacheRefs();
        }

        private void CacheRefs()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (rectTransform == null) rectTransform = transform as RectTransform;
            if (rectTransform != null && rectTransform.localScale != Vector3.zero)
            {
                baseScale = rectTransform.localScale;
            }
            if (targetImage != null)
            {
                targetImage.preserveAspect = true;
                targetImage.raycastTarget = false;
            }
        }

        private void OnEnable()
        {
            CacheRefs();
            if (playOnEnable && frames != null && frames.Length > 0)
            {
                PlayLoop();
            }
            else if (frames != null && frames.Length > 0)
            {
                ApplyFrame(0);
            }
        }

        private void OnDisable()
        {
            isPlaying = false;
            if (oneShotCoroutine != null)
            {
                StopCoroutine(oneShotCoroutine);
                oneShotCoroutine = null;
            }
            if (rectTransform != null)
            {
                rectTransform.localScale = baseScale;
            }
            if (targetImage != null)
            {
                Color c = targetImage.color;
                c.a = 1f;
                targetImage.color = c;
            }
        }

        private void Update()
        {
            if (!isPlaying || oneShotCoroutine != null || frames == null || frames.Length <= 1) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0f) return;

            frameTimer += dt * framesPerSecond;
            if (frameTimer >= 1f)
            {
                int steps = Mathf.FloorToInt(frameTimer);
                frameTimer -= steps;

                if (loop)
                {
                    currentFrame = (currentFrame + steps) % frames.Length;
                }
                else
                {
                    currentFrame = Mathf.Min(currentFrame + steps, frames.Length - 1);
                    if (currentFrame >= frames.Length - 1)
                    {
                        isPlaying = false;
                    }
                }

                ApplyFrame(currentFrame);
            }
        }

        public void SetFrames(Sprite[] newFrames, float fps = -1f, bool shouldLoop = true)
        {
            CacheRefs();
            frames = newFrames;
            if (fps > 0f) framesPerSecond = fps;
            loop = shouldLoop;
            currentFrame = 0;
            frameTimer = 0f;
            if (frames != null && frames.Length > 0)
            {
                ApplyFrame(0);
            }
        }

        public void PlayLoop(float customFps = -1f)
        {
            CacheRefs();
            if (frames == null || frames.Length == 0) return;

            if (oneShotCoroutine != null)
            {
                StopCoroutine(oneShotCoroutine);
                oneShotCoroutine = null;
            }

            if (customFps > 0f) framesPerSecond = customFps;
            loop = true;
            isPlaying = true;
            currentFrame = 0;
            frameTimer = 0f;

            if (targetImage != null)
            {
                Color c = targetImage.color;
                c.a = 1f;
                targetImage.color = c;
                targetImage.enabled = true;
            }
            if (rectTransform != null) rectTransform.localScale = baseScale;

            gameObject.SetActive(true);
            ApplyFrame(0);
        }

        public void StopAndHide()
        {
            isPlaying = false;
            if (oneShotCoroutine != null)
            {
                StopCoroutine(oneShotCoroutine);
                oneShotCoroutine = null;
            }
            gameObject.SetActive(false);
        }

        public void SetNormalizedProgress(float progress01)
        {
            CacheRefs();
            if (frames == null || frames.Length == 0) return;

            isPlaying = false;
            float clamped = Mathf.Clamp01(progress01);
            int idx = Mathf.Clamp(Mathf.FloorToInt(clamped * frames.Length), 0, frames.Length - 1);
            if (clamped >= 0.99f) idx = frames.Length - 1;
            currentFrame = idx;
            if (targetImage != null)
            {
                Color c = targetImage.color;
                c.a = 1f;
                targetImage.color = c;
                targetImage.enabled = true;
            }
            ApplyFrame(idx);
        }

        public void PlayOneShot(float durationSeconds = 0.6f, bool fadeOutTail = true, bool hideWhenDone = true, float startScale = 0.85f, float endScale = 1.12f)
        {
            CacheRefs();
            if (frames == null || frames.Length == 0) return;

            isPlaying = false;
            gameObject.SetActive(true);

            if (oneShotCoroutine != null)
            {
                StopCoroutine(oneShotCoroutine);
            }
            oneShotCoroutine = StartCoroutine(OneShotRoutine(Mathf.Max(0.12f, durationSeconds), fadeOutTail, hideWhenDone, startScale, endScale));
        }

        private IEnumerator OneShotRoutine(float duration, bool fadeOutTail, bool hideWhenDone, float startScale, float endScale)
        {
            if (targetImage != null) targetImage.enabled = true;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += dt;
                float t = Mathf.Clamp01(elapsed / duration);

                int idx = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
                ApplyFrame(idx);

                if (rectTransform != null)
                {
                    float scaleFactor = Mathf.Lerp(startScale, endScale, Mathf.SmoothStep(0f, 1f, t));
                    rectTransform.localScale = baseScale * scaleFactor;
                }

                if (targetImage != null)
                {
                    Color c = targetImage.color;
                    c.a = (fadeOutTail && t > 0.65f) ? Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f) : 1f;
                    targetImage.color = c;
                }

                yield return null;
            }

            if (rectTransform != null) rectTransform.localScale = baseScale;
            if (targetImage != null)
            {
                Color c = targetImage.color;
                c.a = 1f;
                targetImage.color = c;
            }

            oneShotCoroutine = null;
            if (hideWhenDone)
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyFrame(int index)
        {
            if (targetImage == null || frames == null || index < 0 || index >= frames.Length) return;
            Sprite s = frames[index];
            if (s != null)
            {
                targetImage.sprite = s;
            }
        }
    }
}
