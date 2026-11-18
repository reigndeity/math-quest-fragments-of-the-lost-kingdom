using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MathQuest.UI
{
    /// <summary>
    /// Put on any UI element with a raycast target: a horizontal drag across it raises <see cref="Swiped"/>.
    /// Direction is +1 for a swipe to the left (go to the next item) and -1 for a swipe to the right (previous).
    /// Works with a finger or a mouse drag.
    /// </summary>
    public sealed class SwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("How far the drag must travel sideways, as a fraction of the screen width.")]
        [SerializeField, Range(0.02f, 0.5f)] private float minDistance = 0.08f;

        /// <summary>+1 = swiped left (next), -1 = swiped right (previous).</summary>
        public event Action<int> Swiped;

        public void OnBeginDrag(PointerEventData eventData) { }

        // Required so the EventSystem keeps sending drag events to this object.
        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - eventData.pressPosition;

            if (Mathf.Abs(delta.x) < Screen.width * minDistance) return;
            if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return; // mostly vertical, not a swipe

            Swiped?.Invoke(delta.x < 0f ? 1 : -1);
        }
    }
}
