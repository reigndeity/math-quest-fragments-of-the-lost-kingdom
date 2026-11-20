using MathQuest.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// Put on the Yes button of a confirmation: pressing it erases the saved game. Closing the confirmation is
    /// done by the Panel Toggle Button next to it.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ResetSaveButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Reset);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Reset);
        }

        private void Reset() => SaveSystem.Reset();
    }
}
