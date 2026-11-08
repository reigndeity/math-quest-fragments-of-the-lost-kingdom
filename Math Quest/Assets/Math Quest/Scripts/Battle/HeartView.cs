using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// One heart on the HealthHearts prefab. "Filled" shows the Heart Fill child; the
    /// dark Heart BG behind it always stays, so a lost heart reads as an empty slot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeartView : MonoBehaviour
    {
        [Tooltip("The Heart Fill child. Turned off when this heart is lost.")]
        [SerializeField] private GameObject fill;

        public void SetFilled(bool filled)
        {
            if (fill != null)
                fill.SetActive(filled);
        }
    }
}
