using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem.UI
{
    /// <summary>
    /// Plays an animated GIF frame sequence on a UI Image (e.g. Lutuoanbackground frames)
    /// with a calm, cozy playback speed, startup stabilization, and subtle Pan Right ambient drift.
    /// Works across all platforms with zero runtime file decoding overhead.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIAnimatedBackground : MonoBehaviour
    {
        [Header("Frame Animation")]
        [Tooltip("The ordered frames of the animated background.")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("Target playback rate in frames per second. Slower values (6-10 FPS) provide a calm, cozy feel.")]
        [SerializeField] private float framesPerSecond = 8f;

        [Tooltip("Delay in seconds on the initial frame before starting animation playback (prevents startup rush).")]
        [SerializeField] private float startupDelay = 0.35f;

        [Tooltip("Whether to loop the animation continuously.")]
        [SerializeField] private bool loop = true;

        [Tooltip("Whether animation runs during Time.timeScale = 0 (recommended for menus).")]
        [SerializeField] private bool unscaledTime = true;

        [Header("Ambient Pan Right Drift (Cozy Motion)")]
        [Tooltip("Enables a subtle, slow horizontal pan across the background to enhance the cozy atmosphere.")]
        [SerializeField] private bool enableAmbientPan = true;

        [Tooltip("How far the background gently drifts horizontally in pixels.")]
        [SerializeField] private float panDistance = 25f;

        [Tooltip("Speed of the ambient horizontal pan.")]
        [SerializeField] private float panSpeed = 2.0f;

        private Image targetImage;
        private RectTransform rectTransform;
        private Vector2 initialAnchoredPosition;
        private int currentFrameIndex;
        private float frameTimer;
        private float startupTimer;
        private float panTime;
        private bool isStartupComplete;

        public Sprite[] Frames
        {
            get => frames;
            set
            {
                frames = value;
                if (frames != null && frames.Length > 0 && targetImage != null)
                {
                    targetImage.sprite = frames[0];
                }
            }
        }

        public float FramesPerSecond
        {
            get => framesPerSecond;
            set => framesPerSecond = Mathf.Max(0.1f, value);
        }

        private void Awake()
        {
            targetImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                initialAnchoredPosition = rectTransform.anchoredPosition;
            }

            if (frames != null && frames.Length > 0 && targetImage != null)
            {
                targetImage.sprite = frames[0];
            }

            currentFrameIndex = 0;
            frameTimer = 0f;
            startupTimer = 0f;
            isStartupComplete = (startupDelay <= 0f);
        }

        private void Update()
        {
            float rawDt = unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // Clamp delta time to avoid large frame jumps caused by scene loading or engine hitches on startup
            float dt = Mathf.Min(rawDt, 0.05f);

            // Handle initial startup hold so the background doesn't rush or cycle before UI settles
            if (!isStartupComplete)
            {
                startupTimer += dt;
                if (startupTimer >= startupDelay)
                {
                    isStartupComplete = true;
                    frameTimer = 0f;
                }
                else
                {
                    UpdateAmbientPan(dt);
                    return;
                }
            }

            UpdateFrameAnimation(dt);
            UpdateAmbientPan(dt);
        }

        private void UpdateFrameAnimation(float dt)
        {
            if (frames == null || frames.Length <= 1 || targetImage == null || framesPerSecond <= 0f)
            {
                return;
            }

            frameTimer += dt;
            float interval = 1f / framesPerSecond;

            if (frameTimer >= interval)
            {
                frameTimer = 0f; // Reset cleanly to avoid accumulating and skipping frames
                currentFrameIndex++;

                if (currentFrameIndex >= frames.Length)
                {
                    if (loop)
                    {
                        currentFrameIndex = 0;
                    }
                    else
                    {
                        currentFrameIndex = frames.Length - 1;
                        return;
                    }
                }

                if (frames[currentFrameIndex] != null)
                {
                    targetImage.sprite = frames[currentFrameIndex];
                }
            }
        }

        private void UpdateAmbientPan(float dt)
        {
            if (!enableAmbientPan || rectTransform == null || panDistance <= 0f)
            {
                return;
            }

            panTime += dt * (panSpeed * 0.1f);
            float offset = Mathf.Sin(panTime) * panDistance;
            rectTransform.anchoredPosition = new Vector2(initialAnchoredPosition.x + offset, initialAnchoredPosition.y);
        }

        /// <summary>
        /// Resets the background animation and drift to the initial state.
        /// </summary>
        public void ResetAnimation()
        {
            currentFrameIndex = 0;
            frameTimer = 0f;
            startupTimer = 0f;
            isStartupComplete = (startupDelay <= 0f);
            panTime = 0f;

            if (frames != null && frames.Length > 0 && targetImage != null)
            {
                targetImage.sprite = frames[0];
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = initialAnchoredPosition;
            }
        }
    }
}
