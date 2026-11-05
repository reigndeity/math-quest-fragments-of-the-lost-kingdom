using TexDrawLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// A tile in the pool. Lives on the tile prefab. Tapping is handled by the button;
    /// dragging is forwarded to the owning <see cref="ExpressionBuilderInput"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpressionTileView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TEXDraw label;

        private ExpressionBuilderInput owner;
        private string tileId;
        private bool dragging;

        public Button Button => button;

        public void Show(string math)
        {
            label.text = math;
        }

        /// <summary>Connects this view to its input so drags can be reported. Not called on drag ghosts.</summary>
        public void Bind(ExpressionBuilderInput builder, string id)
        {
            owner = builder;
            tileId = id;
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
