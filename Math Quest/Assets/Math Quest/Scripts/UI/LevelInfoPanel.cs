using MathQuest.Bootstrap;
using MathQuest.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// On the AboutCurrentLevelPanel. One panel is reused for every level: <see cref="Show"/> fills it for the
    /// tapped level and opens it, and the Play button starts that level in the Battle scene.
    /// For now it only shows the stars; best time and mistakes wait for saving.
    /// </summary>
    public sealed class LevelInfoPanel : MonoBehaviour
    {
        [Tooltip("The star holders in order: star 1 first (the one that fills first).")]
        [SerializeField] private RectTransform[] stars = System.Array.Empty<RectTransform>();
        [Tooltip("The child of each star holder turned on when that star is earned.")]
        [SerializeField] private string starFillName = "StarFill";

        [Header("Play")]
        [SerializeField] private Button playButton;
        [Tooltip("Gives the act the level belongs to.")]
        [SerializeField] private LevelMapView map;
        [Tooltip("Must be in Build Settings.")]
        [SerializeField] private string battleSceneName = "Battle";

        /// <summary>The level the panel is showing.</summary>
        public LevelNode Current { get; private set; }

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(Play);
        }

        public void Show(LevelNode node)
        {
            Current = node;

            int earned = node != null ? node.Stars : 0;
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;

                Transform fill = stars[i].Find(starFillName);
                if (fill != null) fill.gameObject.SetActive(i < earned);
            }

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>Starts the shown level: remembers it, then swaps this scene for the Battle scene.</summary>
        public void Play()
        {
            if (Current == null || Current.Level == null || map == null || map.Act == null)
                return;

            LevelSession.Set(map.Act, Current.Level);
            SceneLoader.Replace(battleSceneName, gameObject.scene);
        }
    }
}
