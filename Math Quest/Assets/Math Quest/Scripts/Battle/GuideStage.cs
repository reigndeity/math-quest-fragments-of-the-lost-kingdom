using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// The guide book's part of the screen (GuideBookHolder): the book and its speech bubble.
    /// It stays switched off until the guide has something to say. Show switches it on and the
    /// book pops in (scale 0, up past full size, then settles at 1); once the pop is done the
    /// bubble's holder fades in. Hide fades everything out and switches it off.
    /// Put this on GuideBookHolder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GuideStage : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("The book (BookModel). It pops in.")]
        [SerializeField] private RectTransform book;
        [Tooltip("Where the guide's bubbles spawn (GuideBookDialogueHolder). It fades in after the pop.")]
        [SerializeField] private RectTransform dialogueHolder;

        [Header("Pop (seconds)")]
        [Tooltip("How big the book gets at the top of the pop, as a multiple of its normal size.")]
        [SerializeField, Min(1f)] private float popOvershoot = 1.25f;
        [Tooltip("Scale 0 to the overshoot size.")]
        [SerializeField, Min(0.01f)] private float popUpDuration = 0.2f;
        [Tooltip("The overshoot size back to normal.")]
        [SerializeField, Min(0.01f)] private float settleDuration = 0.12f;

        [Tooltip("How long the book takes to shrink away while a practice question is on screen.")]
        [SerializeField, Min(0.01f)] private float hideBookDuration = 0.15f;

        [Header("Fades (seconds)")]
        [Tooltip("The bubble's holder fading in after the pop (and out again before the book leaves).")]
        [SerializeField, Min(0.01f)] private float bubbleFadeInDuration = 0.25f;

        private CanvasGroup group;       // the whole guide, used to fade out
        private CanvasGroup bubbleGroup; // the bubble holder, used to fade in

        private void Awake()
        {
            EnsureReferences();
            gameObject.SetActive(false);
        }

        private void EnsureReferences()
        {
            if (group != null)
                return;

            group = GetOrAdd(gameObject);
            if (dialogueHolder != null)
                bubbleGroup = GetOrAdd(dialogueHolder.gameObject);

            // Never swallow the taps that move the dialogue on.
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private static CanvasGroup GetOrAdd(GameObject target)
        {
            CanvasGroup found = target.GetComponent<CanvasGroup>();
            if (found == null)
                found = target.AddComponent<CanvasGroup>();

            found.interactable = false;
            found.blocksRaycasts = false;
            return found;
        }

        /// <summary>Switches the guide on, pops the book in, then fades the bubble's holder in.</summary>
        public IEnumerator Show()
        {
            EnsureReferences();

            group.alpha = 1f;
            if (bubbleGroup != null)
                bubbleGroup.alpha = 0f;
            if (book != null)
                book.localScale = Vector3.zero;

            gameObject.SetActive(true);

            yield return PopBook();

            if (bubbleGroup != null)
                yield return Fade(bubbleGroup, 1f, bubbleFadeInDuration);
        }

        /// <summary>The book pops in: scale 0, up past full size, then settles at 1.</summary>
        public IEnumerator PopBook()
        {
            if (book == null)
                yield break;

            book.localScale = Vector3.zero;
            yield return Scale(book, 0f, popOvershoot, popUpDuration, EaseOutCubic);
            yield return Scale(book, popOvershoot, 1f, settleDuration, SmoothStep);
        }

        /// <summary>The book shrinks away (used while a question is on screen, so it is not in the way).</summary>
        public IEnumerator ShrinkBook()
        {
            if (book != null)
                yield return Scale(book, book.localScale.x, 0f, hideBookDuration, SmoothStep);
        }

        /// <summary>
        /// Leaves the way it came in, backwards: the bubble's holder fades out, then the book swells a
        /// little and shrinks to nothing. Then the guide is switched off.
        /// </summary>
        public IEnumerator Hide()
        {
            EnsureReferences();

            if (bubbleGroup != null)
                yield return Fade(bubbleGroup, 0f, bubbleFadeInDuration);

            if (book != null)
            {
                yield return Scale(book, book.localScale.x, popOvershoot, settleDuration, SmoothStep);
                yield return Scale(book, popOvershoot, 0f, popUpDuration, EaseInCubic);
            }

            gameObject.SetActive(false);
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        private static float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private static float SmoothStep(float t)
        {
            return Mathf.SmoothStep(0f, 1f, t);
        }

        private static IEnumerator Scale(RectTransform target, float from, float to, float duration, System.Func<float, float> ease)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, ease(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            target.localScale = Vector3.one * to;
        }

        private static IEnumerator Fade(CanvasGroup target, float to, float duration)
        {
            float from = target.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                target.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            target.alpha = to;
        }
    }
}
