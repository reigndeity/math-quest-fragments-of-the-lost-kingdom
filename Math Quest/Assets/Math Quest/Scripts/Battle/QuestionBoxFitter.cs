using TexDrawLib;
using TexDrawLib.Core;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// Sizes the Question Box and its text to fit a question. Put this on QuestionBoxHolder:
    /// the holder's height is the tallest the box may become, and the box shrinks to the
    /// text when the question is short.
    ///
    /// The text starts at Max Font Size and steps down until it fits inside the holder.
    /// If it still does not fit at Min Font Size, TEXDraw shrinks it further as a last
    /// resort so nothing is cut off.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestionBoxFitter : MonoBehaviour
    {
        private enum VerticalAlign { Top, Middle, Bottom }

        [Header("References")]
        [Tooltip("The Question Box (the Image that holds the text).")]
        [SerializeField] private RectTransform box;
        [Tooltip("The TEXDraw label inside the Question Box.")]
        [SerializeField] private TEXDraw text;

        [Header("Font size (canvas units)")]
        [SerializeField, Min(1f)] private float maxFontSize = 90f;
        [SerializeField, Min(1f)] private float minFontSize = 40f;
        [Tooltip("How much smaller each attempt is. Smaller steps fit more tightly but cost a little more.")]
        [SerializeField, Min(0.5f)] private float fontStep = 2f;

        [Header("Box")]
        [Tooltip("The box never gets shorter than this, however short the question is.")]
        [SerializeField, Min(0f)] private float minBoxHeight = 200f;
        [Tooltip("Where the box sits inside the holder when it is shorter than the holder.")]
        [SerializeField] private VerticalAlign align = VerticalAlign.Top;

        private RectTransform Holder => (RectTransform)transform;

        // The question currently shown, and the settings it was fitted with. While the box is
        // visible, changing any value (or resizing the holder) refits it, so the Inspector
        // can be used to tune it live in Play mode.
        private string shownQuestion;
        private (float max, float min, float step, float minBox, VerticalAlign align, float holderHeight) fittedWith;

        /// <summary>
        /// Activates the holder and box and sizes them to the question. Follow with Conceal() to
        /// keep it invisible until its turn, then Reveal() to play its own activation animation.
        /// </summary>
        public void Prepare(string question)
        {
            gameObject.SetActive(true);
            box.gameObject.SetActive(true);
            Fit(question);
        }

        /// <summary>Prepare and show in one go (no waiting for Reveal).</summary>
        public void Show(string question)
        {
            Prepare(question);
        }

        /// <summary>Deactivates the holder but keeps the size it was fitted to.</summary>
        public void Conceal()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Activates the holder again, which plays its animation.</summary>
        public void Reveal()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            shownQuestion = null;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (shownQuestion == null || !box.gameObject.activeInHierarchy)
                return;

            if (CurrentSettings() != fittedWith)
                Fit(shownQuestion);
        }

        private (float, float, float, float, VerticalAlign, float) CurrentSettings()
        {
            return (maxFontSize, minFontSize, fontStep, minBoxHeight, align, Holder.rect.height);
        }

        public void Fit(string question)
        {
            shownQuestion = question;
            fittedWith = CurrentSettings();
            ApplyAnchors();

            float maxHeight = Mathf.Max(minBoxHeight, Holder.rect.height);

            // Measure at the full holder height so the text width is known.
            box.sizeDelta = new Vector2(0f, maxHeight);

            // The text is stretched inside the box; its offsets are the padding.
            Vector2 inset = -text.rectTransform.sizeDelta;

            text.text = question;
            text.overflow = Overflow.Visible; // measure the real height, do not shrink while measuring

            float chosen = minFontSize;
            float boxHeight = maxHeight;

            for (float size = maxFontSize; size >= minFontSize; size -= fontStep)
            {
                float needed = Measure(size) + inset.y;
                if (needed <= maxHeight)
                {
                    chosen = size;
                    boxHeight = needed;
                    break;
                }
            }

            box.sizeDelta = new Vector2(0f, Mathf.Clamp(boxHeight, minBoxHeight, maxHeight));
            text.size = chosen;
            text.overflow = Overflow.BestFit; // safety net if even the minimum size does not fit
        }

        // Height TEXDraw needs for the current text at this size, at the text's current width.
        private float Measure(float size)
        {
            text.size = size;
            text.CalculateLayoutInputHorizontal();
            text.CalculateLayoutInputVertical();
            return text.preferredHeight;
        }

        private void ApplyAnchors()
        {
            float y = align == VerticalAlign.Top ? 1f : align == VerticalAlign.Middle ? 0.5f : 0f;
            box.anchorMin = new Vector2(0f, y);
            box.anchorMax = new Vector2(1f, y);
            box.pivot = new Vector2(0.5f, y);
            box.anchoredPosition = Vector2.zero;
        }

        private void OnValidate()
        {
            if (minFontSize > maxFontSize)
                minFontSize = maxFontSize;
        }
    }
}
