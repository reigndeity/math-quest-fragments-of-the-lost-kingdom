using System.Collections;
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

        [Header("Open and close animation (optional)")]
        [Tooltip("The Animator on PauseHolder. It plays its Active state when the panel opens (the Paused bool is on) and its Inactive state when Paused turns off. It should use Unscaled Time, because the game is frozen while paused.")]
        [SerializeField] private Animator pauseAnimator;
        [SerializeField] private string pausedParameter = "Paused";
        [Tooltip("The state that plays while closing. The panel is switched off once it has finished.")]
        [SerializeField] private string inactiveStateName = "State_Inactive";

        [Header("Background blur (optional)")]
        [Tooltip("The BlurBackdropController. The background blurs while the game is paused.")]
        [SerializeField] private BlurBackdropController blur;

        [Header("Level requirements (optional)")]
        [Tooltip("Gives the level, whose star times are shown below. Also hides the question while paused.")]
        [SerializeField] private BattleController battle;
        [Tooltip("The question and answers fade out while paused, so the question cannot be studied while the clock is stopped.")]
        [SerializeField] private bool hideQuestionWhilePaused = true;
        [SerializeField, Min(0.01f)] private float questionHideDuration = 0.25f;
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
        [Tooltip("On: leaving for the menu plays the loading transition (the circle).")]
        [SerializeField] private bool useTransition = true;

        private Button pauseButton;
        private bool blurredByPause; // only fade out a blur this pause started
        private Coroutine closeRoutine;

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
            Haptics.Cancel(); // the shake stops with the pause, so the buzz does too

            RefreshRequirements();

            // If a question is open the background is already blurred; leave that blur alone.
            blurredByPause = blur != null && !blur.IsBlurred;
            if (blurredByPause) blur.FadeIn();

            if (hideQuestionWhilePaused && battle != null)
                battle.SetQuestionHiddenByPause(true, questionHideDuration);

            // Paused again while the panel was still closing: stop that and start the opening over.
            if (closeRoutine != null)
            {
                StopCoroutine(closeRoutine);
                closeRoutine = null;
            }

            if (settingsHolder != null) settingsHolder.SetActive(false);
            if (confirmationHolder != null) confirmationHolder.SetActive(false);

            if (pausePanel != null)
            {
                if (pausePanel.activeSelf) pausePanel.SetActive(false); // switching it on again restarts its Animator
                pausePanel.SetActive(true);
            }

            if (pauseAnimator != null) pauseAnimator.SetBool(pausedParameter, true);
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

            if (blurredByPause && blur != null) blur.FadeOut();
            blurredByPause = false;

            if (hideQuestionWhilePaused && battle != null)
                battle.SetQuestionHiddenByPause(false, questionHideDuration);

            // The panel plays its closing animation (the game is already running again), then switches off.
            if (pauseAnimator != null && pausePanel != null && pausePanel.activeInHierarchy)
            {
                pauseAnimator.SetBool(pausedParameter, false);
                closeRoutine = StartCoroutine(HidePanelAfterClose());
            }
            else
            {
                HidePanelNow();
            }
        }

        private void HidePanelNow()
        {
            if (settingsHolder != null) settingsHolder.SetActive(false);
            if (confirmationHolder != null) confirmationHolder.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private IEnumerator HidePanelAfterClose()
        {
            yield return null; // the Animator moves to its Inactive state on the next update

            // Wait for the closing state to finish, but never longer than a couple of seconds.
            float waited = 0f;
            while (waited < 2f)
            {
                AnimatorStateInfo info = pauseAnimator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName(inactiveStateName) && info.normalizedTime >= 1f)
                    break;

                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            closeRoutine = null;
            HidePanelNow();
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
            SceneLoader.Replace(menuSceneName, gameObject.scene, useTransition);
        }
    }
}
