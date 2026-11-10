using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// What a tile or slot view needs from whatever owns it, so the same views work for the
    /// original builder input and the new ExpressionBuilderPanel.
    /// </summary>
    public interface IExpressionDragHost
    {
        /// <summary>Starts dragging a tile from the pool. Returns false if dragging is not allowed.</summary>
        bool BeginTileDrag(string tileId, Vector2 screenPoint, Camera eventCamera);

        /// <summary>Starts dragging the tile in a slot. Returns false for empty or locked slots.</summary>
        bool BeginSlotDrag(int slotIndex, Vector2 screenPoint, Camera eventCamera);

        void UpdateDrag(Vector2 screenPoint, Camera eventCamera);

        void EndDrag(Vector2 screenPoint, Camera eventCamera);
    }
}
