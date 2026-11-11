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

        /// <summary>
        /// Animation event with no parameter. Put it on the frame of the attack animation where
        /// the effect should appear. Which skill plays is decided by the battle (the attacker's
        /// skill for the question's difficulty).
        /// </summary>
        public void PlaySkill()
        {
            SkillRequested?.Invoke();
        }
    }
}
