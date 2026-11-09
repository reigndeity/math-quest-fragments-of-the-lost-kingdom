using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// Sizes the answer area to its content and stacks the submit area right underneath it.
    /// Put this on AnswerAndSubmitHolder. With 4 options the answer area is short and the
    /// submit area sits close under it; with 6 options both grow and the submit area moves down.
    ///
    /// With a "Below" rect set (the question box), the stack starts just under it and follows
    /// it as it grows or shrinks. Without one, the stack is aligned inside the holder instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnswerAreaFitter : MonoBehaviour
    {
        private enum VerticalAlign { Top, Middle, Bottom }

        [Header("References")]
        [Tooltip("The area the answer inputs are spawned into.")]
        [SerializeField] private RectTransform answerArea;
        [Tooltip("The submit area. Optional: leave empty if there is none. When it is inactive it is left out of the stack.")]
        [SerializeField] private RectTransform submitArea;

        [Header("Follow the question box")]
        [Tooltip("Optional. Drag the QuestionBox here: the stack then starts just under it and follows it as it resizes. Align is ignored while this is set.")]
        [SerializeField] private RectTransform below;
        [Tooltip("Space between the bottom of that box and the answer area.")]
        [SerializeField] private float gapBelow = 40f;

        [Header("Layout (canvas units)")]
        [Tooltip("The answer area never gets shorter than this, however few options there are.")]
        [SerializeField, Min(0f)] private float minAnswerHeight = 200f;
        [Tooltip("Space between the answer area and the submit area.")]
        [SerializeField] private float gap = 25f;
        [Tooltip("Used only when Below is empty: where the stack sits inside the holder. Top keeps the answers where they are and lets the submit area move up.")]
        [SerializeField] private VerticalAlign align = VerticalAlign.Top;

        private RectTransform Holder => (RectTransform)transform;

        private float contentHeight;
        private bool hasContent;
        private bool followPaused;
        private readonly Vector3[] corners = new Vector3[4];
        private (float, float, float, VerticalAlign, float, bool, float, float, float) fittedWith;

        /// <summary>
        /// Call after the answer input has been spawned, with the height its content needs
        /// (for example the grid's preferred height).
        /// </summary>
        public void Fit(float height)
        {
            contentHeight = height;
            hasContent = true;
            Apply();
        }

        public void Clear()
        {
            hasContent = false;
        }

        /// <summary>
        /// While paused the stack does not follow the question box. Pause it while the question
        /// box animates in, otherwise the answers would slide along with it.
        /// </summary>
        public void SetFollowPaused(bool paused)
        {
            followPaused = paused;
        }

        // Re-applies when a value is tuned in the Inspector, or the holder or submit area is resized.
        private void Update()
        {
            if (hasContent && !followPaused && Key() != fittedWith)
                Apply();
        }

        private (float, float, float, VerticalAlign, float, bool, float, float, float) Key()
        {
            bool submitActive = submitArea != null && submitArea.gameObject.activeSelf;
            return (contentHeight, minAnswerHeight, gap, align, Holder.rect.height,
                    submitActive, submitActive ? submitArea.rect.height : 0f,
                    below != null ? BelowEdge() : 0f, gapBelow);
        }

        // Where the bottom of the "Below" rect is, measured down from the holder's top edge.
        private float BelowEdge()
        {
            below.GetWorldCorners(corners); // 0 is the bottom left corner
            return Holder.InverseTransformPoint(corners[0]).y - Holder.rect.yMax;
        }

        private void Apply()
        {
            fittedWith = Key();

            bool submitActive = submitArea != null && submitArea.gameObject.activeSelf;

            // Read the sizes before the anchors change, since rect depends on them.
            float answerWidth = answerArea.rect.width;
            float submitWidth = submitActive ? submitArea.rect.width : 0f;
            float submitHeight = submitActive ? submitArea.rect.height : 0f;

            float answerHeight = Mathf.Max(minAnswerHeight, contentHeight);
            float total = answerHeight + (submitActive ? gap + submitHeight : 0f);

            float holderHeight = Holder.rect.height;
            float top;
            if (below != null)
                top = BelowEdge() - gapBelow; // right under the question box
            else
                top = align == VerticalAlign.Top ? 0f
                    : align == VerticalAlign.Middle ? -(holderHeight - total) * 0.5f
                    : -(holderHeight - total);

            PlaceFromTop(answerArea, top, answerWidth, answerHeight);
            if (submitActive)
                PlaceFromTop(submitArea, top - answerHeight - gap, submitWidth, submitHeight);
        }

        // Anchors the rect to the top centre of the holder and sets its top edge and size.
        private static void PlaceFromTop(RectTransform rect, float topEdge, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topEdge);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
