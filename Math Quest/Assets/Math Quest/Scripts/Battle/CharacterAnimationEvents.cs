using System;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// Receives animation events from the Animator it sits next to (PlayerModel or EnemyModel).
    /// Animation events can only call methods on the same object as the Animator, so this is
    /// where an attack animation says "now" and the battle answers by playing the skill.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationEvents : MonoBehaviour
    {
        /// <summary>Raised by the PlaySkill animation event.</summary>
        public event Action SkillRequested;

        private Canvas canvas;
        private int defaultSortingOrder;
        private bool sortingChanged;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            if (canvas != null)
                defaultSortingOrder = canvas.sortingOrder;
        }

        /// <summary>
        /// Animation event with no parameter. Put it on the frame of the attack animation where
        /// the effect should appear. Which skill plays is decided by the battle (the attacker's
        /// skill for the question's difficulty).
        /// </summary>
        public void PlaySkill()
        {
            SkillRequested?.Invoke();
        }

        /// <summary>
        /// Animation event with a whole number (Int). Changes this character's drawing order, for example
        /// to jump in front of the other character mid-attack. The Canvas on this object (PlayerModel or
        /// EnemyModel) must have Override Sorting on. The battle puts it back when the attack is over.
        /// </summary>
        public void SetSortingOrder(int order)
        {
            if (canvas == null)
            {
                Debug.LogWarning($"{name} has no Canvas, so SetSortingOrder does nothing. Add one with Override Sorting on.", this);
                return;
            }

            canvas.sortingOrder = order;
            sortingChanged = true;
        }

        /// <summary>Animation event with no parameter. Puts the drawing order back to what it was before the attack.</summary>
        public void ResetSortingOrder()
        {
            if (canvas != null && sortingChanged)
            {
                canvas.sortingOrder = defaultSortingOrder;
                sortingChanged = false;
            }
        }
    }
}
