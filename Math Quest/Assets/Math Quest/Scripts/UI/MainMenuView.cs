using MathQuest.Bootstrap;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// On the MainMenuPanel. The start button says New Game until a character and name have been saved, then
    /// Continue. New Game opens the character selection; Continue goes straight on to the act selection.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private string newGameText = "New Game";
        [SerializeField] private string continueText = "Continue";

        [Tooltip("On: pressing the button plays the loading transition (the circle) while the next panel opens.")]
        [SerializeField] private bool useTransition = true;

        [Header("Where the button goes")]
        [Tooltip("Opened by New Game.")]
        [SerializeField] private GameObject characterSelectionPanel;
        [Tooltip("Opened by Continue.")]
        [SerializeField] private GameObject actSelectionPanel;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(OnStartPressed);
        }

        private void OnEnable()
        {
            SaveSystem.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            SaveSystem.Changed -= Refresh;
        }

        private void Refresh()
        {
            if (startLabel != null)
                startLabel.text = SaveSystem.HasSave ? continueText : newGameText;
        }

        private void OnStartPressed()
        {
            GameObject next = SaveSystem.HasSave ? actSelectionPanel : characterSelectionPanel;
            if (next == null)
                return;

            if (useTransition)
                ScreenTransition.Run(() => Switch(next));
            else
                Switch(next);
        }

        private void Switch(GameObject next)
        {
            gameObject.SetActive(false);
            next.SetActive(true);
        }
    }
}
