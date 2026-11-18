using UnityEngine;

namespace MathQuest.UI
{
    /// <summary>
    /// On the SettingPanel root. Each scene's instance ticks <see cref="showResetButton"/>:
    /// on in the Menu scene, off everywhere else (e.g. Battle).
    /// </summary>
    public class SettingPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject resetGameDataButton;
        [SerializeField] private bool showResetButton;

        private void Awake() => Apply();

        private void Apply()
        {
            if (resetGameDataButton != null) resetGameDataButton.SetActive(showResetButton);
        }
    }
}
