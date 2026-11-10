using TexDrawLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// A tile in the pool. Lives on the tile prefab. Tapping is handled by the button;
    /// dragging is forwarded to the owner (an <see cref="IExpressionDragHost"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpressionTileView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TEXDraw label;

        private IExpressionDragHost owner;
        private string tileId;
        private bool dragging;
        private CanvasGroup availability;

        public Button Button => button;

        public void Show(string math)
        {
            label.text = math;
        }

        /// <summary>Connects this view to its input so drags can be reported. Not called on drag ghosts.</summary>
        public void Bind(IExpressionDragHost builder, string id)
        {
            owner = builder;
            tileId = id;
        }

        /// <summary>
        /// Shows or hides the tile while keeping its place in the pool, so the pool does not
        /// reflow when a tile is placed in a slot.
        /// </summary>
        public void SetAvailable(bool available)
        {
            if (availability == null)
            {
                availability = GetComponent<CanvasGroup>();
                if (availability == null)
                    availability = gameObject.AddComponent<CanvasGroup>();
            }

            availability.alpha = available ? 1f : 0f;
            availability.blocksRaycasts = available;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = owner != null && owner.BeginTileDrag(tileId, eventData.position, eventData.pressEventCamera);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging)
                owner.UpdateDrag(eventData.position, eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;

            dragging = false;
            owner.EndDrag(eventData.position, eventData.pressEventCamera);
        }
    }
}
