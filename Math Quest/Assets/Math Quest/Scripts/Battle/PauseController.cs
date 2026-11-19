using MathQuest.Bootstrap;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// On the PauseButton in the Battle scene. Pressing it opens the pause canvas and freezes the battle;
    /// Resume closes it and carries on. Settings and Menu open their own panels inside the pause canvas, and
    /// confirming Menu leaves the battle for the menu scene.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class PauseController : MonoBehaviour
    {
        [Header("Pause panel")]
        [Tooltip("The PausePanel inside UiCanvas. UiCanvas stays on because the match end panel lives in it too.")]
        [FormerlySerializedAs("pauseCanvas")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;

        [Header("Level requirements (optional)")]
        [Tooltip("Gives the level, whose star times are shown below.")]
        [SerializeField] private BattleController battle;
        [SerializeField] private TMP_Text twoStarTimeText;
        [SerializeField] private TMP_Text threeStarTimeText;

        [Header("Settings")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsHolder;

        [Header("Back to menu")]
        [SerializeField] private Button menuButton;
        [SerializeField] private GameObject confirmationHolder;
        [SerializeField] private Button confirmButton;
        [Tooltip("Must be in Build Settings.")]
        [SerializeField] private string menuSceneName = "Menu";

        private Button pauseButton;

        public bool IsPaused { get; private set; }

        private void Awake()
        {
            pauseButton = GetComponent<Button>();
            pauseButton.onClick.AddListener(Pause);

            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (menuButton != null) menuButton.onClick.AddListener(OpenMenuConfirmation);
            if (confirmButton != null) confirmButton.onClick.AddListener(LeaveToMenu);
        }

        private void OnDestroy()
        {
            if (pauseButton != null) pauseButton.onClick.RemoveListener(Pause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (menuButton != null) menuButton.onClick.RemoveListener(OpenMenuConfirmation);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(LeaveToMenu);

            // Never leave the game frozen for the next scene.
            if (IsPaused) Time.timeScale = 1f;
        }

        public void Pause()
        {
            if (IsPaused) return;

            IsPaused = true;
            Time.timeScale = 0f;

            RefreshRequirements();

            if (settingsHolder != null) settingsHolder.SetActive(false);
            if (confirmationHolder != null) confirmationHolder.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        /// <summary>Called when the match ends: the pause button stops working (and an open pause closes).</summary>
        public void DisablePausing()
        {
            Resume();

            // Awake has not run yet if the button's canvas was still off.
            if (pauseButton == null) pauseButton = GetComponent<Button>();
            pauseButton.interactable = false;
        }

        private void RefreshRequirements()
        {
            LevelResource level = battle != null ? battle.Level : null;
            if (level == null) return;

            if (twoStarTimeText != null) twoStarTimeText.text = FormatMinutes(level.twoStarTime);
            if (threeStarTimeText != null) threeStarTimeText.text = FormatMinutes(level.threeStarTime);
        }

        private static string FormatMinutes(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        public void Resume()
        {
            if (!IsPaused) return;

            IsPaused = false;
            Time.timeScale = 1f;

            if (settingsHolder != null) settingsHolder.SetActive(false);
            if (confirmationHolder != null) confirmationHolder.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void OpenSettings()
        {
            if (settingsHolder != null) settingsHolder.SetActive(true);
        }

        private void OpenMenuConfirmation()
        {
            if (confirmationHolder != null) confirmationHolder.SetActive(true);
        }

        private void LeaveToMenu()
        {
            IsPaused = false;
            Time.timeScale = 1f;

            LevelSession.Clear();
            SceneLoader.Replace(menuSceneName, gameObject.scene);
        }
    }
}
