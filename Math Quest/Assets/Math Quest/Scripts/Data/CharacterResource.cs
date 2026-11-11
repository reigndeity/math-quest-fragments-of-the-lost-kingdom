using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>The skill this character uses against questions of one difficulty.</summary>
    [Serializable]
    public sealed class DifficultySkill
    {
        public QuestionDifficulty difficulty;
        public SkillResource skill;
    }

    /// <summary>
    /// One fighter: who they are, how many hearts they start with, and how they look in battle.
    /// The player is a special case: its name will later come from what the player types, and
    /// Display Name is only the default.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterResource_Enemy",
        menuName = "Math Quest/Resources/Character Resource")]
    public sealed class CharacterResource : ScriptableObject
    {
        [Tooltip("Player or Enemy. Decides which side of the battle scene this character is shown on.")]
        public CharacterRole role = CharacterRole.Enemy;

        [Tooltip("Shown in the character's stats box. For the player this is only the default name.")]
        public string displayName = "NAME";

        [Tooltip("How many hearts this character starts the battle with.")]
        [Min(1)] public int maxHearts = 3;

        [Tooltip("The character prefab (for example Player_Boy). It is spawned into the PlayerModel or EnemyModel holder when the battle starts. If empty, whatever is already in the holder is kept.")]
        public GameObject characterPrefab;

        [Tooltip("The skill used for each question difficulty: the player uses it when answering right, the enemy when the player answers wrong. A difficulty with no entry plays no effect.")]
        public List<DifficultySkill> skills = new();

        /// <summary>The skill for a difficulty, or null if none is set.</summary>
        public SkillResource GetSkill(QuestionDifficulty difficulty)
        {
            foreach (DifficultySkill entry in skills)
            {
                if (entry != null && entry.difficulty == difficulty)
                    return entry.skill;
            }

            return null;
        }
    }
}
