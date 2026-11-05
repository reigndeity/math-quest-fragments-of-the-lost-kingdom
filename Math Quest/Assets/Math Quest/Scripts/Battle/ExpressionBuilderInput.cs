using System;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MathQuest.Battle
{
    /// <summary>
    /// Input for questions answered by dragging tiles into slots. Drag a tile from the pool
    /// onto a slot; drag a tile from one slot to another to move or swap; drag a tile from a
    /// slot back onto the pool to remove it. Dropping anywhere else cancels. Reports the
    /// placed tile ids and does not decide whether they are correct.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpressionBuilderInput : MonoBehaviour
    {
        [SerializeField] private RectTransform slotContainer;
        [SerializeField] private RectTransform tileContainer;
        [SerializeField] private ExpressionSlotView slotPrefab;
        [SerializeField] private ExpressionTileView tilePrefab;
        [SerializeField] private Button submitButton;

        [Header("Dragging")]
        [Tooltip("Parent for the tile that follows the finger. Leave empty to use this object.")]
        [SerializeField] private RectTransform dragLayer;
        [Tooltip("Offset of the dragged tile from the finger, so a thumb does not hide it.")]
        [SerializeField] private Vector2 ghostOffset = new(0f, 90f);

        [Header("Slot colors")]
        [SerializeField] private Color emptySlotColor = new(0.78f, 0.78f, 0.78f, 1f);
        [SerializeField] private Color filledSlotColor = Color.white;
        [SerializeField] private Color lockedSlotColor = new(0.7f, 0.85f, 1f, 1f);
        [SerializeField] private Color hoverSlotColor = new(0.7f, 1f, 0.7f, 1f);

        private readonly List<ExpressionSlotView> slotViews = new();
        private readonly Dictionary<string, ExpressionTileView> tileViews = new();

        private ExpressionBuilderAnswerResource answer;
        private Action<IReadOnlyList<string>> onSubmitted;
        private string[] placed = Array.Empty<string>();
        private bool[] locked = Array.Empty<bool>();
        private bool interactable = true;

        // Drag state. dragFromSlot is -1 when the tile came from the pool.
        private GameObject ghost;
        private string dragTileId;
        private int dragFromSlot = -1;
        private int hoverSlot = -1;

        private RectTransform DragLayer => dragLayer != null ? dragLayer : (RectTransform)transform;

        private void Awake()
        {
            submitButton.onClick.AddListener(Submit);
        }

        private void OnDisable()
        {
            ResetDrag();
        }

        public void Show(ExpressionBuilderAnswerResource answerResource, Action<IReadOnlyList<string>> submitted)
        {
            answer = answerResource;
            onSubmitted = submitted;
            interactable = true;
            ResetDrag();

            ClearViews();
            BuildSlots();
            BuildTiles();
            Refresh();

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool value)
        {
            interactable = value;

            foreach (ExpressionSlotView slot in slotViews)
                slot.Button.interactable = value;

            foreach (ExpressionTileView tile in tileViews.Values)
                tile.Button.interactable = value;

            Refresh();
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

        /// <summary>Starts dragging a tile from the pool. Returns false if dragging is not allowed.</summary>
        public bool BeginTileDrag(string tileId, Vector2 screenPoint, Camera eventCamera)
        {
            return BeginDrag(tileId, -1, screenPoint, eventCamera);
        }

        /// <summary>Starts dragging the tile in a slot. Returns false for empty or locked slots.</summary>
        public bool BeginSlotDrag(int slotIndex, Vector2 screenPoint, Camera eventCamera)
        {
            if (slotIndex < 0 || slotIndex >= placed.Length || locked[slotIndex] || placed[slotIndex] == null)
                return false;

            return BeginDrag(placed[slotIndex], slotIndex, screenPoint, eventCamera);
        }

        public void UpdateDrag(Vector2 screenPoint, Camera eventCamera)
        {
            if (ghost == null)
                return;

            MoveGhost(screenPoint, eventCamera);

            int over = SlotAt(screenPoint, eventCamera);
            hoverSlot = over >= 0 && !locked[over] ? over : -1;
            Refresh();
        }

        public void EndDrag(Vector2 screenPoint, Camera eventCamera)
        {
            if (ghost == null)
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
        }

        private bool BeginDrag(string tileId, int fromSlot, Vector2 screenPoint, Camera eventCamera)
        {
            if (!interactable || answer == null || !answer.TryGetTile(tileId, out ExpressionTile tile))
                return false;

            ResetDrag();
            dragTileId = tileId;
            dragFromSlot = fromSlot;

            ExpressionTileView ghostView = Instantiate(tilePrefab, DragLayer);
            ghostView.Show(tile.math); // Never bound, so the ghost cannot start drags of its own.

            CanvasGroup group = ghostView.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.alpha = 0.9f;

            ghost = ghostView.gameObject;
            ghost.transform.SetAsLastSibling();
            MoveGhost(screenPoint, eventCamera);

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

        private bool AllSlotsFilled()
        {
            foreach (string tileId in placed)
            {
                if (tileId == null)
                    return false;
            }

            return true;
        }

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

            foreach (KeyValuePair<string, ExpressionTileView> pair in tileViews)
                pair.Value.gameObject.SetActive(Array.IndexOf(placed, pair.Key) < 0);

            submitButton.interactable = interactable && AllSlotsFilled();
        }

        private void Submit()
        {
            if (!interactable || !AllSlotsFilled())
                return;

            onSubmitted?.Invoke((string[])placed.Clone());
        }
    }
}
