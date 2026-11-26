using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Shows who is asking the question (a picture on top of the Question Box), so the player knows
    /// whether a wrong answer hurts. Put this on QuestionIndicator, next to its CanvasGroup.
    /// It is invisible after <see cref="Prepare"/>; <see cref="Play"/> then fades it in while it slides
    /// down from above into the position it has in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class QuestionIndicator : MonoBehaviour
    {
        [Tooltip("The Image that shows the asker (AskerIcon).")]
        [SerializeField] private Image icon;

        [Header("Slide in (seconds / canvas units)")]
        [Tooltip("How far above its resting position it starts.")]
        [SerializeField, Min(0f)] private float slideDistance = 80f;
        [SerializeField, Min(0.01f)] private float duration = 0.35f;

        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 restingPosition;
        private bool hasPicture;
        private Coroutine playing;

        private void EnsureReferences()
        {
            if (group != null)
                return;

            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            restingPosition = rect.anchoredPosition; // the position set in the scene
        }

        /// <summary>
        /// Gets the indicator ready for a question, invisible and sitting above its resting position.
        /// An empty <paramref name="asker"/> means nothing is shown for this question.
        /// </summary>
        public void Prepare(Sprite asker)
        {
            EnsureReferences();

            if (playing != null)
            {
                StopCoroutine(playing);
                playing = null;
            }

            hasPicture = asker != null;
            if (icon != null)
            {
                icon.sprite = asker;
                icon.enabled = hasPicture;
            }

            group.alpha = 0f;
            rect.anchoredPosition = restingPosition + Vector2.up * slideDistance;
        }

        /// <summary>Fades in and slides down to the resting position. Does nothing when there is no picture.</summary>
        public void Play()
        {
            EnsureReferences();

            if (!hasPicture || !isActiveAndEnabled)
                return;

            if (playing != null)
                StopCoroutine(playing);

            playing = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            Vector2 from = restingPosition + Vector2.up * slideDistance;
            float elapsed = 0f;

            // Unscaled time, like the rest of the question panel, so pausing never freezes it half way.
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t) * (1f - t); // fast at first, settles softly

                group.alpha = t;
                rect.anchoredPosition = Vector2.LerpUnclamped(from, restingPosition, eased);
                yield return null;
            }

            group.alpha = 1f;
            rect.anchoredPosition = restingPosition;
            playing = null;
        }
    }
}
