using System;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// On the ActSelectionPanel. Shows one act at a time (ActId order) and lets the player move between them
    /// with the previous / next buttons or by swiping the island. Updates the act label, the locked tint,
    /// the button interactability and the indicator dots.
    /// </summary>
    public sealed class ActSelectionView : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text actLabel;
        [SerializeField] private string labelFormat = "ACT {0}";
        [Tooltip("Shown while the current act is locked.")]
        [SerializeField] private GameObject lockedTint;

        [Header("Navigation")]
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private SwipeHandler swipe;
        [Tooltip("Can only be pressed while the current act is unlocked.")]
        [SerializeField] private Button proceedButton;

        [Header("Indicator dots (one per act, in order)")]
        [SerializeField] private Image[] indicators;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = Color.black;

        [Header("Unlocking")]
        [Tooltip("Placeholder until saving exists: how many acts, counted from Act 1, are unlocked.")]
        [SerializeField, Min(1)] private int unlockedActCount = 1;

        private static readonly ActId[] Acts = (ActId[])Enum.GetValues(typeof(ActId));

        private int index;

        /// <summary>The act currently shown.</summary>
        public ActId CurrentAct => Acts[index];

        public bool IsCurrentActLocked => index >= unlockedActCount;

        /// <summary>Raised whenever the shown act changes.</summary>
        public event Action<ActId> ActChanged;

        private void Awake()
        {
            if (previousButton != null) previousButton.onClick.AddListener(Previous);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (swipe != null) swipe.Swiped += OnSwiped;
        }

        private void OnDestroy()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(Previous);
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
            if (swipe != null) swipe.Swiped -= OnSwiped;
        }

        private void OnEnable() => Refresh();

        public void Next() => Move(1);

        public void Previous() => Move(-1);

        private void OnSwiped(int direction) => Move(direction);

        private void Move(int step)
        {
            int target = Mathf.Clamp(index + step, 0, Acts.Length - 1);
            if (target == index) return;

            index = target;
            Refresh();
            ActChanged?.Invoke(CurrentAct);
        }

        private void Refresh()
        {
            if (actLabel != null) actLabel.text = string.Format(labelFormat, (int)CurrentAct);
            if (lockedTint != null) lockedTint.SetActive(IsCurrentActLocked);

            if (proceedButton != null) proceedButton.interactable = !IsCurrentActLocked;
            if (previousButton != null) previousButton.interactable = index > 0;
            if (nextButton != null) nextButton.interactable = index < Acts.Length - 1;

            if (indicators == null) return;
            for (int i = 0; i < indicators.Length; i++)
            {
                if (indicators[i] == null) continue;
                indicators[i].gameObject.SetActive(i < Acts.Length);
                indicators[i].color = i == index ? activeColor : inactiveColor;
            }
        }
    }
}
