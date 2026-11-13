using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// The live blur behind the answer panel, made with Unified Universal Blur (a package).
    ///
    /// How it fits together:
    /// - The Universal Blur Feature on Renderer2D blurs the whole camera image every frame (the battle
    ///   Canvas: background, ambience, characters), so nothing behind the panel pauses.
    /// - AnswerPanel is on its own Screen Space - Overlay Canvas. The camera never draws it, so the blur
    ///   cannot see (and re-blur) itself.
    /// - BlurBackDrop is a full-screen Image with the UniversalBlurUI material. It shows that blur; its
    ///   Color tints it (grey darkens it). This component fades it in and out.
    ///
    /// How blurry it is lives on the Universal Blur Feature (Iterations, Downsample, Scale, Offset).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlurBackdropController : MonoBehaviour
    {
        [Tooltip("The full-screen Image with the UniversalBlurUI material (BlurBackDrop), the first child of AnswerPanel so it sits behind the question and answers. Its Color tints the blur.")]
        [SerializeField] private Graphic image;
        [Tooltip("How long the blur takes to fade in.")]
        [SerializeField, Min(0.01f)] private float fadeInDuration = 0.5f;
        [Tooltip("How long the blur takes to fade out when no duration is given. During a battle, BattleController passes its own Blur Fade Out Duration instead.")]
        [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.8f;

        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;

        private CanvasGroup group;
        private Coroutine fade;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (group != null)
                return;

            if (image == null)
            {
                Debug.LogError("BlurBackdropController needs the Image (BlurBackDrop) assigned.", this);
                return;
            }

            // Fading the group blends between the sharp live battle (0) and the full blur (1).
            group = image.GetComponent<CanvasGroup>();
            if (group == null)
                group = image.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            group.interactable = false;
            image.enabled = false;
        }

        /// <summary>
        /// Gets the blur ready before it fades in. The blur is live, so nothing is captured any more:
        /// this switches the backdrop on (still invisible) and waits a frame. Kept so the battle's call
        /// order stays the same.
        /// </summary>
        public IEnumerator Capture()
        {
            EnsureInitialized();
            if (group == null) yield break;

            StopFade();
            group.alpha = 0f;
            image.enabled = true;
            yield return null;
        }

        /// <summary>Fades the blur in. Without a duration, Fade In Duration is used.</summary>
        public void FadeIn(float duration = -1f)
        {
            EnsureInitialized();
            if (group == null) return;

            image.enabled = true;
            StartFade(1f, false, duration >= 0f ? duration : fadeInDuration);
        }

        /// <summary>Fades the blur out. Without a duration, Fade Out Duration is used.</summary>
        public void FadeOut(float duration = -1f)
        {
            EnsureInitialized();
            if (group == null) return;

            StartFade(0f, true, duration >= 0f ? duration : fadeOutDuration);
        }

        private void StartFade(float target, bool hideWhenDone, float duration)
        {
            StopFade();
            fade = StartCoroutine(FadeRoutine(target, hideWhenDone, Mathf.Max(0.01f, duration)));
        }

        private void StopFade()
        {
            if (fade != null) StopCoroutine(fade);
            fade = null;
        }

        private IEnumerator FadeRoutine(float target, bool hideWhenDone, float duration)
        {
            float from = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            group.alpha = target;
            if (hideWhenDone) image.enabled = false;
            fade = null;
        }

        [ContextMenu("Test: fade in")]
        private void TestFadeIn()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Press Play first.", this);
                return;
            }

            StartCoroutine(TestRoutine());
        }

        [ContextMenu("Test: fade out")]
        private void TestFadeOut()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Press Play first.", this);
                return;
            }

            FadeOut();
        }

        private IEnumerator TestRoutine()
        {
            yield return Capture();
            FadeIn();
        }
    }
}
