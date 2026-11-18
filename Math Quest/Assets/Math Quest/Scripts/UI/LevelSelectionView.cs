using UnityEngine;

namespace MathQuest.UI
{
    /// <summary>
    /// On the LevelSelectionPanel. Opens the level info panel when a level is tapped, and resets the
    /// panel's pop-ups every time the screen is opened.
    /// </summary>
    public sealed class LevelSelectionView : MonoBehaviour
    {
        [SerializeField] private LevelMapView map;
        [SerializeField] private LevelInfoPanel infoPanel;
        [Tooltip("Optional. The Return confirmation, closed whenever the screen opens.")]
        [SerializeField] private GameObject confirmation;

        private void Awake()
        {
            if (map != null) map.LevelClicked += OnLevelClicked;
        }

        private void OnDestroy()
        {
            if (map != null) map.LevelClicked -= OnLevelClicked;
        }

        private void OnEnable()
        {
            if (infoPanel != null) infoPanel.Hide();
            if (confirmation != null) confirmation.SetActive(false);
        }

        private void OnLevelClicked(LevelNode node)
        {
            if (infoPanel != null) infoPanel.Show(node);
        }
    }
}
