using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// On the NameInputPanel. The Confirm button only works once something other than spaces is typed.
    /// Confirming remembers the name. Which panel opens next is set up with Panel Toggle Buttons on the
    /// Confirm and Cancel buttons.
    /// </summary>
    public sealed class NameInputView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [Tooltip("Can only be pressed while a name is typed.")]
        [SerializeField] private Button confirmButton;

        /// <summary>The typed name without leading or trailing spaces.</summary>
        public string EnteredName => inputField != null ? inputField.text.Trim() : string.Empty;

        private void Awake()
        {
            if (inputField != null) inputField.onValueChanged.AddListener(OnTextChanged);
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
        }

        private void OnDestroy()
        {
            if (inputField != null) inputField.onValueChanged.RemoveListener(OnTextChanged);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
        }

        private void OnEnable() => Refresh();

        private void OnTextChanged(string _) => Refresh();

        private void Confirm()
        {
            if (EnteredName.Length == 0) return;

            // A character and a name are both chosen now, so this is where the game is first saved.
            PlayerProfile.PlayerName = EnteredName;
            SaveSystem.StartNewGame(PlayerProfile.Character, EnteredName);
        }

        private void Refresh()
        {
            if (confirmButton != null) confirmButton.interactable = EnteredName.Length > 0;
        }
    }
}
