using System;
using System.Collections;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MathQuest.Battle
{
    /// <summary>
    /// The ExpressionBuilderHolder prefab: questions answered by dragging tiles into slots.
    /// Drag a tile from the pool onto a slot; drag a tile from one slot to another to move or
    /// swap; drag a tile from a slot back onto the pool to remove it. Dropping anywhere else
    /// cancels. There is no submit button of its own: the battle's one SubmitButton hands the
    /// answer in. It does not decide whether the answer is correct.
    ///
    /// A tile placed in a slot is hidden in the pool, not switched off, so the pool never
    /// reflows and the answer area never changes height while the player drags.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpressionBuilderPanel : MonoBehaviour, IExpressionDragHost
    {
        [SerializeField] private RectTransform slotContainer;
        [SerializeField] private RectTransform tileContainer;
        [SerializeField] private ExpressionSlotView slotPrefab;
        [SerializeField] private ExpressionTileView tilePrefab;

        [Header("Dragging")]
        [Tooltip("The tile follows the finger while it is dragged. Off: nothing follows the finger, only the slot under it lights up. Either way the tile's place in the pool is hidden while it is held.")]
        [SerializeField] private bool showDraggedTile = true;
        [Tooltip("Covers the whole panel and is ignored by the layout. The dragged tile lives here.")]
        [SerializeField] private RectTransform dragLayer;
        [Tooltip("Offset of the dragged tile from the finger, so a thumb does not hide it. Only used when Show Dragged Tile is on.")]
        [SerializeField] private Vector2 ghostOffset = new(0f, 90f);

        [Header("Slot colors")]
        [SerializeField] private Color emptySlotColor = new(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField] private Color filledSlotColor = Color.white;
        [SerializeField] private Color lockedSlotColor = new(0.7f, 0.85f, 1f, 1f);
        [SerializeField] private Color hoverSlotColor = new(0.7f, 1f, 0.7f, 1f);

        private readonly List<ExpressionSlotView> slotViews = new();
        private readonly Dictionary<string, ExpressionTileView> tileViews = new();

        private CanvasGroup group;
        private Animator animator;
        private ExpressionBuilderAnswerResource answer;
        private Action changed;
        private string[] placed = Array.Empty<string>();
        private bool[] locked = Array.Empty<bool>();
        private bool interactable;

        // Drag state. dragFromSlot is -1 when the tile came from the pool.
        private bool dragging;
        private GameObject ghost; // the tile that follows the finger; only exists while dragging with Show Dragged Tile on
        private string dragTileId;
        private int dragFromSlot = -1;
        private int hoverSlot = -1;

        private RectTransform DragLayer => dragLayer != null ? dragLayer : (RectTransform)transform;

        /// <summary>True when every slot has a tile (prefilled slots count).</summary>
        public bool AllFilled
        {
            get
            {
                foreach (string tileId in placed)
                {
                    if (tileId == null)
                        return false;
                }

                return placed.Length > 0;
            }
        }

        /// <summary>The tile id in every slot, in slot order. Null entries are empty slots.</summary>
        public IReadOnlyList<string> PlacedTileIds => (string[])placed.Clone();

        /// <summary>
        /// The built expression as the player's answer line: every placed tile's math, in slot
        /// order, for example "$\frac{x^2-4}{3y+2}$ $+$ $[2(x-y)]^3$".
        /// </summary>
        public string AnswerText
        {
            get
            {
                var parts = new List<string>();
                foreach (string tileId in placed)
                {
                    if (tileId != null && answer != null && answer.TryGetTile(tileId, out ExpressionTile tile))
                        parts.Add(tile.math);
                }

                return string.Join(" ", parts);
            }
        }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            animator = GetComponent<Animator>();
        }

        private void OnDisable()
        {
            ResetDrag();
        }

        /// <summary>
        /// Builds the slots and the tile pool. <paramref name="onChanged"/> fires whenever the
        /// placement changes.
        /// </summary>
        public void Show(ExpressionBuilderAnswerResource answerResource, Action onChanged)
        {
            answer = answerResource;
            changed = onChanged;
            interactable = true;
            ResetDrag();

            ClearViews();
            BuildSlots();
            BuildTiles();
            Refresh();
        }

        /// <summary>
        /// Hides the panel without taking it out of the layout, and stops it taking input. If the
        /// prefab has an Animator its intro animation is held back until Reveal().
        /// </summary>
        public void Conceal()
        {
            if (animator != null)
                animator.enabled = false;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        /// <summary>
        /// Lets the panel take input and shows it: by playing its Animator's intro animation if it
        /// has one, otherwise by fading in over <paramref name="fadeDuration"/>.
        /// </summary>
        public void Reveal(float fadeDuration)
        {
            group.interactable = true;
            group.blocksRaycasts = true;

            if (animator != null)
                animator.enabled = true; // starts its state from the beginning
            else
                StartCoroutine(FadeIn(fadeDuration));
        }

        /// <summary>Stops the tiles and slots reacting, for example once the answer is submitted.</summary>
        public void Lock()
        {
            interactable = false;
            group.interactable = false;
            ResetDrag();
            Refresh();
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                yield return null;
            }

            group.alpha = 1f;
        }

        private void ClearViews()
        {
            foreach (ExpressionSlotView slot in slotViews)
                Destroy(slot.gameObject);
            foreach (ExpressionTileView tile in tileViews.Values)
                Destroy(tile.gameObject);

            slotViews.Clear();
            tileViews.Clear();
        }

        private void BuildSlots()
        {
            int count = answer.Slots.Count;
            placed = new string[count];
            locked = new bool[count];

            for (int i = 0; i < count; i++)
            {
                ExpressionSlotView view = Instantiate(slotPrefab, slotContainer);
                view.Bind(this, i);
                slotViews.Add(view);

                string prefilled = answer.Slots[i].prefilledTileId;
                if (!string.IsNullOrEmpty(prefilled))
                {
                    placed[i] = prefilled;
                    locked[i] = true;
                }
            }
        }

        private void BuildTiles()
        {
            // Tiles locked into prefilled slots are not offered in the pool.
            var pool = new List<ExpressionTile>();
            foreach (ExpressionTile tile in answer.Tiles)
            {
                if (tile != null && Array.IndexOf(placed, tile.id) < 0)
                    pool.Add(tile);
            }

            // Shuffle so the pool order does not hint at the answer.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int swapWith = Random.Range(0, i + 1);
                (pool[i], pool[swapWith]) = (pool[swapWith], pool[i]);
            }

            foreach (ExpressionTile tile in pool)
            {
                ExpressionTileView view = Instantiate(tilePrefab, tileContainer);
                view.Show(tile.math);
                view.Bind(this, tile.id);
                tileViews[tile.id] = view;
            }
        }

        // ---------------------------------------------------------------- dragging

        public bool BeginTileDrag(string tileId, Vector2 screenPoint, Camera eventCamera)
        {
            return BeginDrag(tileId, -1, screenPoint, eventCamera);
        }

        public bool BeginSlotDrag(int slotIndex, Vector2 screenPoint, Camera eventCamera)
        {
            if (slotIndex < 0 || slotIndex >= placed.Length || locked[slotIndex] || placed[slotIndex] == null)
                return false;

            return BeginDrag(placed[slotIndex], slotIndex, screenPoint, eventCamera);
        }

        public void UpdateDrag(Vector2 screenPoint, Camera eventCamera)
        {
            if (!dragging)
                return;

            if (ghost != null)
                MoveGhost(screenPoint, eventCamera);

            int over = SlotAt(screenPoint, eventCamera);
            hoverSlot = over >= 0 && !locked[over] ? over : -1;
            Refresh();
        }

        public void EndDrag(Vector2 screenPoint, Camera eventCamera)
        {
            if (!dragging)
                return;

            int target = SlotAt(screenPoint, eventCamera);
            bool overSlot = target >= 0 && !locked[target];

            if (dragFromSlot < 0)
            {
                if (overSlot)
                    placed[target] = dragTileId; // A tile already there returns to the pool.
            }
            else if (overSlot)
            {
                if (target != dragFromSlot)
                    (placed[dragFromSlot], placed[target]) = (placed[target], placed[dragFromSlot]);
            }
            else if (RectangleContains(tileContainer, screenPoint, eventCamera))
            {
                placed[dragFromSlot] = null; // Dropped back on the pool.
            }
            // Anywhere else cancels: nothing changes.

            ResetDrag();
            Refresh();
            changed?.Invoke();
        }

        private bool BeginDrag(string tileId, int fromSlot, Vector2 screenPoint, Camera eventCamera)
        {
            if (!interactable || answer == null || !answer.TryGetTile(tileId, out ExpressionTile tile))
                return false;

            ResetDrag();
            dragging = true;
            dragTileId = tileId;
            dragFromSlot = fromSlot;

            if (showDraggedTile)
            {
                ExpressionTileView ghostView = Instantiate(tilePrefab, DragLayer);
                ghostView.Show(tile.math); // Never bound, so the ghost cannot start drags of its own.

                CanvasGroup ghostGroup = ghostView.gameObject.AddComponent<CanvasGroup>();
                ghostGroup.blocksRaycasts = false;
                ghostGroup.alpha = 0.9f;

                ghost = ghostView.gameObject;
                ghost.transform.SetAsLastSibling();
                MoveGhost(screenPoint, eventCamera);
            }

            Refresh();
            return true;
        }

        private void MoveGhost(Vector2 screenPoint, Camera eventCamera)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    DragLayer, screenPoint, eventCamera, out Vector2 local))
            {
                ((RectTransform)ghost.transform).anchoredPosition = local + ghostOffset;
            }
        }

        private void ResetDrag()
        {
            if (ghost != null)
                Destroy(ghost);

            ghost = null;
            dragging = false;
            dragTileId = null;
            dragFromSlot = -1;
            hoverSlot = -1;
        }

        private int SlotAt(Vector2 screenPoint, Camera eventCamera)
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                if (RectangleContains((RectTransform)slotViews[i].transform, screenPoint, eventCamera))
                    return i;
            }

            return -1;
        }

        private static bool RectangleContains(RectTransform rect, Vector2 screenPoint, Camera eventCamera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, eventCamera);
        }

        // ---------------------------------------------------------------- shared

        private void Refresh()
        {
            for (int i = 0; i < slotViews.Count; i++)
            {
                bool dragSource = dragFromSlot == i; // Looks empty while its tile is being dragged.

                string math = string.Empty;
                if (!dragSource && placed[i] != null && answer.TryGetTile(placed[i], out ExpressionTile tile))
                    math = tile.math;

                Color color = locked[i] ? lockedSlotColor
                    : dragSource ? emptySlotColor
                    : hoverSlot == i ? hoverSlotColor
                    : placed[i] != null ? filledSlotColor
                    : emptySlotColor;

                slotViews[i].Show(math, color);
            }

            // Placed tiles are hidden, not removed, so the pool keeps its shape.
            // A tile that is in a slot, or is being dragged from the pool, leaves an empty spot in the
            // pool instead of a visible copy. It is hidden, not removed, so the pool keeps its shape.
            foreach (KeyValuePair<string, ExpressionTileView> pair in tileViews)
            {
                bool placedInSlot = Array.IndexOf(placed, pair.Key) >= 0;
                bool held = dragging && dragFromSlot < 0 && dragTileId == pair.Key;
                pair.Value.SetAvailable(!placedInSlot && !held);
            }
        }
    }
}
