using UnityEngine;

namespace MathQuest.UI
{
    /// <summary>Lets another scene (the Battle scene) ask the Menu scene to open on the level selection screen.</summary>
    public static class MenuNavigation
    {
        public static bool OpenLevelSelection { get; set; }
    }

    /// <summary>
    /// On the Menu scene's Canvas. When the scene starts after a battle, it opens the level selection panel
    /// instead of the main menu. Otherwise nothing changes.
    /// </summary>
    public sealed class MenuStartRouter : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject levelSelectionPanel;

        private void Awake()
        {
            if (!MenuNavigation.OpenLevelSelection)
                return;

            MenuNavigation.OpenLevelSelection = false;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (levelSelectionPanel != null) levelSelectionPanel.SetActive(true);
        }
    }
}
