using System.Collections.Generic;
using MathQuest.Bootstrap;
using MathQuest.Data;
using MathQuest.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// On UiCanvas. Opens the MatchEndPanel when the battle is over (after the enemy dissolves, or after the
    /// player's last words) and fills it: Victory or Defeat, this run's answering time, the best time, and the
    /// stars earned. Restart plays the same level again; Return goes back to the level selection.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchEndController : MonoBehaviour
    {
        // Until saving exists, the best time only lasts while the game is running.
        private static readonly Dictionary<LevelResource, float> SessionBestTimes = new();

        [SerializeField] private BattleController battle;
        [SerializeField] private PauseController pause;
        [Tooltip("The BlurBackdropController. The background blurs while the match end panel is open.")]
        [SerializeField] private BlurBackdropController blur;

        [Header("Panel")]
        [Tooltip("The MatchEndPanel. It is turned on when the battle ends.")]
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private string victoryText = "Victory";
        [SerializeField] private string defeatText = "Defeat";

        [Header("Times")]
        [SerializeField] private TMP_Text currentTimeValue;
        [SerializeField] private TMP_Text bestTimeValue;

        [Header("Stars")]
        [Tooltip("One per star, star 1 first. Each is turned on when that star is earned.")]
        [SerializeField] private GameObject[] starFills = System.Array.Empty<GameObject>();

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button returnToLevelSelectionButton;
        [Tooltip("Must be in Build Settings.")]
        [SerializeField] private string menuSceneName = "Menu";

        private void Awake()
        {
            if (matchEndPanel != null) matchEndPanel.SetActive(false);

            if (battle != null) battle.BattleEnded += OnBattleEnded;
            if (restartButton != null) restartButton.onClick.AddListener(Restart);
            if (returnToLevelSelectionButton != null) returnToLevelSelectionButton.onClick.AddListener(ReturnToLevelSelection);
        }

        private void OnDestroy()
        {
            if (battle != null) battle.BattleEnded -= OnBattleEnded;
            if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
            if (returnToLevelSelectionButton != null) returnToLevelSelectionButton.onClick.RemoveListener(ReturnToLevelSelection);
        }

        private void OnBattleEnded(bool playerWon, float answeringTime)
        {
            LevelResource level = battle.Level;

            if (pause != null) pause.DisablePausing();

            int stars = playerWon && level != null ? level.StarsForTime(answeringTime) : 0;

            float best = 0f;
            bool hasBest = level != null && SessionBestTimes.TryGetValue(level, out best);
            if (playerWon && level != null && (!hasBest || answeringTime < best))
            {
                best = answeringTime;
                hasBest = true;
                SessionBestTimes[level] = best;
            }

            if (title != null) title.text = playerWon ? victoryText : defeatText;
            if (currentTimeValue != null) currentTimeValue.text = Format(answeringTime);
            if (bestTimeValue != null) bestTimeValue.text = hasBest ? Format(best) : "--:--";

            for (int i = 0; i < starFills.Length; i++)
            {
                if (starFills[i] != null)
                    starFills[i].SetActive(i < stars);
            }

            if (blur != null && !blur.IsBlurred) blur.FadeIn();
            if (matchEndPanel != null) matchEndPanel.SetActive(true);
        }

        private void Restart()
        {
            // The level stays as it is: LevelSession (or the Inspector's level) is read again by the new battle.
            SceneLoader.Reload(gameObject.scene);
        }

        private void ReturnToLevelSelection()
        {
            LevelSession.Clear();
            MenuNavigation.OpenLevelSelection = true;
            SceneLoader.Replace(menuSceneName, gameObject.scene);
        }

        private static string Format(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
