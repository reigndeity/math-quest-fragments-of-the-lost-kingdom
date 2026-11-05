using TexDrawLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// A slot in the expression being built. Lives on the slot prefab. Tapping is handled
    /// by the button; dragging the tile out of the slot is forwarded to the owning
    /// <see cref="ExpressionBuilderInput"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpressionSlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TEXDraw label;

        private ExpressionBuilderInput owner;
        private int slotIndex;
        private bool dragging;

        public Button Button => button;

        public void Show(string math, Color backgroundColor)
        {
            label.text = math ?? string.Empty;
            background.color = backgroundColor;
        }

        /// <summary>Connects this view to its input so drags can be reported.</summary>
        public void Bind(ExpressionBuilderInput builder, int index)
        {
            owner = builder;
            slotIndex = index;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = owner != null && owner.BeginSlotDrag(slotIndex, eventData.position, eventData.pressEventCamera);
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
