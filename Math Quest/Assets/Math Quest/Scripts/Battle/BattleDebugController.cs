using MathQuest.Data;
using TexDrawLib;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// The battle's dev tools in one place. Put it on a GameObject called DebugController and assign it
    /// on the BattleController. While that object is active the tools work; deactivate it (or remove it)
    /// and the battle plays normally.
    /// </summary>
    public sealed class BattleDebugController : MonoBehaviour
    {
        [Header("Answer hint")]
        [Tooltip("Shows the correct answer of the current question on screen (and in the Console), so you do not have to remember them.")]
        [SerializeField] private bool showAnswerHint = true;
        [Tooltip("The AnswerHint prefab. Its color, size, position and font are set on the prefab. Empty: a plain one is created in code.")]
        [SerializeField] private TEXDraw answerHintPrefab;

        [Header("Force a skill")]
        [Tooltip("If set, the enemy always uses this skill when the player answers wrong, instead of picking one at random.")]
        [SerializeField] private SkillResource enemySkill;
        [Tooltip("If set, the player always uses this skill when answering right, whatever the question's difficulty.")]
        [SerializeField] private SkillResource playerSkill;

        /// <summary>True while this object is active and Show Answer Hint is on.</summary>
        public bool ShowAnswerHint => isActiveAndEnabled && showAnswerHint;

        public TEXDraw AnswerHintPrefab => answerHintPrefab;

        /// <summary>The skill to use instead of the random one for this attacker, or null for normal play.</summary>
        public SkillResource ForcedSkill(CharacterRole attacker)
        {
            if (!isActiveAndEnabled)
                return null;

            return attacker == CharacterRole.Enemy ? enemySkill : playerSkill;
        }
    }
}
