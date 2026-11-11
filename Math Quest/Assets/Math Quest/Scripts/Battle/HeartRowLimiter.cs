using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Put on HeartHolder next to a Grid Layout Group. Hearts fill one row until Max Per Row,
    /// then wrap to the next row. With fewer hearts than that the row is only as wide as the
    /// hearts, so the stats box around it can stay as small as possible.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class HeartRowLimiter : MonoBehaviour
    {
        [Tooltip("Most hearts in one row. The next heart starts a new row.")]
        [SerializeField, Min(1)] private int maxPerRow = 6;

        private GridLayoutGroup grid;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();
        private void OnTransformChildrenChanged() => Apply();

        private void Apply()
        {
            if (grid == null)
                grid = GetComponent<GridLayoutGroup>();

            if (grid == null)
                return;

            int count = 0;
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                    count++;
            }

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            int columns = Mathf.Clamp(count, 1, Mathf.Max(1, maxPerRow));
            if (grid.constraintCount != columns)
                grid.constraintCount = columns;
        }
    }
}
