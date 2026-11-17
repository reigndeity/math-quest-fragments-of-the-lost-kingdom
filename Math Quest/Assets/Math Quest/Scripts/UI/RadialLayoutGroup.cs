using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// Lays its children out on a circle (or an ellipse) instead of a row or grid. Works in Edit mode
    /// too, like Unity's own layout groups: add children, change the settings, and they move at once.
    ///
    /// The circle fits the RectTransform (an ellipse if it is not square), or uses a fixed radius.
    /// Children are spread over an arc that starts at the Origin; a full 360 arc spaces them evenly
    /// around the whole circle. Inactive children and children with a Layout Element set to Ignore
    /// Layout are skipped.
    /// </summary>
    [AddComponentMenu("Layout/Radial Layout Group")]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RadialLayoutGroup : UIBehaviour, ILayoutGroup
    {
        public enum Origin { Right, Top, Left, Bottom }
        public enum Direction { Clockwise, CounterClockwise }
        public enum Alignment
        {
            /// <summary>The first child sits on the Origin; the rest follow in the Direction.</summary>
            Start,
            /// <summary>The group is centred on the Origin.</summary>
            Center
        }

        public enum Spread
        {
            /// <summary>Children spread evenly over the whole Arc, however many there are.</summary>
            FillArc,
            /// <summary>Children are Spacing degrees apart, squeezed only if they would not fit in the Arc.</summary>
            FixedSpacing
        }

        [Header("Circle")]
        [Tooltip("Off: the circle fits this RectTransform (an ellipse if it is not square). On: Radius is used.")]
        [SerializeField] private bool useFixedRadius;
        [Tooltip("Distance from the centre to the children's pivots, in UI units. Only used when Use Fixed Radius is on.")]
        [SerializeField, Min(0f)] private float radius = 200f;
        [Tooltip("Moves the children in (negative) or out (positive) from the circle, in UI units.")]
        [SerializeField] private float radiusOffset;
        [Tooltip("On: each child's middle sits on the circle, whatever its pivot. Off: its pivot does (like Unity's other layout groups).")]
        [SerializeField] private bool centerOnCircle = true;

        [Header("Where they go")]
        [Tooltip("Which side of the circle the layout starts from.")]
        [SerializeField] private Origin origin = Origin.Top;
        [Tooltip("Turns the start point around the circle, in degrees.")]
        [SerializeField] private float originOffset;
        [Tooltip("How much of the circle the children may use, in degrees. 360 is the full circle.")]
        [SerializeField, Range(0f, 360f)] private float arc = 360f;
        [SerializeField] private Spread spread = Spread.FillArc;
        [Tooltip("Degrees between neighbours. Only used with Spread: Fixed Spacing.")]
        [SerializeField, Range(0f, 360f)] private float spacing = 30f;
        [SerializeField] private Direction direction = Direction.Clockwise;
        [SerializeField] private Alignment alignment = Alignment.Start;
        [Tooltip("Lays the children out from last to first.")]
        [SerializeField] private bool reverseOrder;

        [Header("Rotation")]
        [Tooltip("On: each child turns to face outwards from the centre (its top points away from the middle).")]
        [SerializeField] private bool rotateChildren;
        [Tooltip("Extra rotation for every child, in degrees. Only used when Rotate Children is on.")]
        [SerializeField] private float rotationOffset;

        private readonly List<RectTransform> children = new();
        private readonly List<ILayoutIgnorer> ignorers = new();
        private DrivenRectTransformTracker tracker;
        private RectTransform rectTransform;

        private RectTransform Rect => rectTransform != null ? rectTransform : rectTransform = (RectTransform)transform;

        // ---- settings from code ----

        public float Radius { get => radius; set { radius = Mathf.Max(0f, value); SetDirty(); } }
        public float Arc { get => arc; set { arc = Mathf.Clamp(value, 0f, 360f); SetDirty(); } }
        public float Spacing { get => spacing; set { spacing = Mathf.Clamp(value, 0f, 360f); SetDirty(); } }
        public float OriginOffset { get => originOffset; set { originOffset = value; SetDirty(); } }

        // ---- Unity callbacks ----

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnDisable()
        {
            tracker.Clear();
            LayoutRebuilder.MarkLayoutForRebuild(Rect);
            base.OnDisable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties() => SetDirty();

        private void OnTransformChildrenChanged() => SetDirty();

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirty();
        }
#endif

        // ---- ILayoutGroup: the layout system calls these when something changed ----

        public void SetLayoutHorizontal() => Arrange();

        public void SetLayoutVertical() { } // everything is placed in one go in SetLayoutHorizontal

        private void Arrange()
        {
            CollectChildren();
            int count = children.Count;
            if (count == 0)
                return;

            float step = StepDegrees(count);
            float sign = direction == Direction.Clockwise ? -1f : 1f; // angles grow counter-clockwise
            float start = OriginDegrees() + originOffset;
            if (alignment == Alignment.Center)
                start -= sign * step * (count - 1) / 2f;

            Vector2 radii = Radii();

            for (int i = 0; i < count; i++)
            {
                RectTransform child = children[reverseOrder ? count - 1 - i : i];
                float angle = (start + sign * step * i) * Mathf.Deg2Rad;
                var direction2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // Anchored to the middle of this rect, so the position below is from the centre.
                child.anchorMin = child.anchorMax = new Vector2(0.5f, 0.5f);
                Vector2 point = new Vector2(direction2D.x * radii.x, direction2D.y * radii.y);

                // The anchored position is where the pivot goes. Shift it so the child's middle lands on the
                // circle instead, so a child with an off-centre pivot is not pushed off the circle.
                if (centerOnCircle)
                    point += Vector2.Scale(child.pivot - new Vector2(0.5f, 0.5f), child.rect.size);

                child.anchoredPosition = point;

                if (rotateChildren)
                    child.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f + rotationOffset);
            }
        }

        // Degrees between neighbours.
        private float StepDegrees(int count)
        {
            if (count <= 1)
                return 0f;

            // A full circle has as many gaps as children (the last one wraps to the first); an arc has one fewer.
            bool fullCircle = arc >= 359.99f;
            float fit = arc / (fullCircle ? count : count - 1);
            return spread == Spread.FillArc ? fit : Mathf.Min(spacing, fit);
        }

        private Vector2 Radii()
        {
            Vector2 r = useFixedRadius ? new Vector2(radius, radius) : Rect.rect.size / 2f;
            return new Vector2(Mathf.Max(0f, r.x + radiusOffset), Mathf.Max(0f, r.y + radiusOffset));
        }

        private float OriginDegrees() => origin switch
        {
            Origin.Right => 0f,
            Origin.Top => 90f,
            Origin.Left => 180f,
            _ => 270f
        };

        private void CollectChildren()
        {
            children.Clear();
            tracker.Clear();

            DrivenTransformProperties driven = DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition;
            if (rotateChildren)
                driven |= DrivenTransformProperties.Rotation;

            for (int i = 0; i < Rect.childCount; i++)
            {
                if (Rect.GetChild(i) is not RectTransform child || !child.gameObject.activeInHierarchy || Ignored(child))
                    continue;

                children.Add(child);
                tracker.Add(this, child, driven); // greys these fields out in the child's Inspector
            }
        }

        private bool Ignored(RectTransform child)
        {
            child.GetComponents(ignorers);
            foreach (ILayoutIgnorer ignorer in ignorers)
            {
                if (ignorer.ignoreLayout)
                    return true;
            }

            return false;
        }

        private void SetDirty()
        {
            if (!IsActive())
                return;

            LayoutRebuilder.MarkLayoutForRebuild(Rect);
        }

#if UNITY_EDITOR
        // Shows the circle and the arc in the Scene view while selected.
        private void OnDrawGizmosSelected()
        {
            Vector2 radii = Radii();
            float start = OriginDegrees() + originOffset;
            float sign = direction == Direction.Clockwise ? -1f : 1f;
            if (alignment == Alignment.Center && arc < 359.99f)
                start -= sign * arc / 2f; // a centred group uses the arc on both sides of the origin
            const int segments = 64;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Vector3 previous = PointOnCircle(start, radii);
            for (int s = 1; s <= segments; s++)
            {
                Vector3 next = PointOnCircle(start + sign * arc * s / segments, radii);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }

            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            Gizmos.DrawLine(Rect.TransformPoint(Rect.rect.center), PointOnCircle(start, radii)); // where the first child goes
        }

        private Vector3 PointOnCircle(float degrees, Vector2 radii)
        {
            float a = degrees * Mathf.Deg2Rad;
            Vector2 local = Rect.rect.center + new Vector2(Mathf.Cos(a) * radii.x, Mathf.Sin(a) * radii.y);
            return Rect.TransformPoint(local);
        }
#endif
    }
}
