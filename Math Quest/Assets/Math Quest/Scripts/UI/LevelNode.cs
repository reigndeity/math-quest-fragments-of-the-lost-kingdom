using System;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MathQuest.UI
{
    public enum LevelNodeState
    {
        /// <summary>Not reached yet (or no level made for this spot). Shows Locked; cannot be selected.</summary>
        Locked,
        /// <summary>Reached but not finished: the current level. Stars show empty.</summary>
        Unlocked,
        /// <summary>Finished at least once. Stars show the best result; selecting it shows its stats.</summary>
        Completed
    }

    /// <summary>
    /// One level on the level map (the Level prefab). Turns its children on and off for its state:
    ///
    ///                          Locked   Unlocked            Completed
    ///   Locked                 on       off                 off
    ///   LevelStarLayoutHolder  off      on (empty stars)    on (best stars filled)
    ///   Selected               off      on when selected    on when selected
    ///   LevelStatsHolder       off      off                 on when selected
    ///
    /// Tapping an unlocked or completed level selects it (LevelMapView keeps one selected at a time).
    /// It never opens the level; a Proceed button will do that.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelNode : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text numberText;

        [Header("Shown by state")]
        [SerializeField] private GameObject lockedObject;
        [SerializeField] private GameObject selectedObject;
        [Tooltip("Optional. The \"you are here\" marker: shown only on the current (unlocked, not yet finished) level.")]
        [SerializeField] private GameObject pointerObject;
        [SerializeField] private GameObject statsHolder;
        [SerializeField] private GameObject starHolder;

        [Header("Stars")]
        [Tooltip("The star holders from left to right. Star 1 fills first.")]
        [SerializeField] private RectTransform[] stars = Array.Empty<RectTransform>();
        [Tooltip("The child of each star holder turned on when that star is earned.")]
        [SerializeField] private string starFillName = "StarFill";

        [Header("Stats")]
        [SerializeField] private TMP_Text timeText;
        [Tooltip("{0} is replaced by the best time as hours:minutes:seconds.")]
        [SerializeField] private string timeFormat = "Time Taken: {0}";

        /// <summary>Tapped while unlocked or completed.</summary>
        public event Action<LevelNode> Clicked;

        /// <summary>Tapped while locked. Nothing listens yet: a hook for a later "locked" shake or sound.</summary>
        public event Action<LevelNode> LockedClicked;

        public int Index { get; private set; }
        public int Number => Index + 1;
        public LevelResource Level { get; private set; }
        public LevelNodeState State { get; private set; } = LevelNodeState.Locked;
        public int Stars { get; private set; }
        public float BestTime { get; private set; }
        public bool IsSelected { get; private set; }
        public bool CanSelect => State != LevelNodeState.Locked;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Setup(int index, LevelResource level)
        {
            Index = index;
            Level = level;
            if (numberText != null)
                numberText.text = Number.ToString();
            Refresh();
        }

        /// <param name="stars">Best stars earned (Completed only).</param>
        /// <param name="bestTime">Best answering time in seconds (Completed only).</param>
        public void SetProgress(LevelNodeState state, int stars = 0, float bestTime = 0f)
        {
            State = state;
            Stars = state == LevelNodeState.Completed ? Mathf.Clamp(stars, 0, this.stars.Length) : 0;
            BestTime = state == LevelNodeState.Completed ? bestTime : 0f;
            if (!CanSelect)
                IsSelected = false;
            Refresh();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected && CanSelect;
            Refresh();
        }

        // A locked level's Button is not interactable, so its click only arrives here.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (State == LevelNodeState.Locked)
                LockedClicked?.Invoke(this);
        }

        private void Refresh()
        {
            bool locked = State == LevelNodeState.Locked;
            bool completed = State == LevelNodeState.Completed;

            SetActive(lockedObject, locked);
            SetActive(starHolder, !locked);
            SetActive(selectedObject, IsSelected);
            SetActive(pointerObject, State == LevelNodeState.Unlocked);
            SetActive(statsHolder, IsSelected && completed);

            if (button != null)
                button.interactable = !locked;

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null)
                    continue;

                Transform fill = stars[i].Find(starFillName);
                if (fill != null)
                    fill.gameObject.SetActive(i < Stars);
            }

            if (timeText != null && completed)
                timeText.text = string.Format(timeFormat, FormatTime(BestTime));
        }

        public static string FormatTime(float seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
