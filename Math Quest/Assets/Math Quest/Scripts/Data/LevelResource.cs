using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>A pool used by a level, with an optional number of questions just for this level.</summary>
    [Serializable]
    public sealed class LevelQuestionPool
    {
        public QuestionPoolResource pool;

        [Tooltip("On: this level uses Question Count below instead of the number set on the pool itself.")]
        public bool overrideCount;

        [Tooltip("How many questions this level takes from the pool. Only used when Override Count is on.")]
        [Min(0)] public int questionCount = 1;
    }

    /// <summary>
    /// One level of an Act. For now it owns the dialogue that plays before the battle and
    /// the questions asked in it. Who speaks first is simply the order of the lines.
    /// Enemy and end dialogue will join this resource later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelResource_Act1_1",
        menuName = "Math Quest/Resources/Level Resource")]
    public sealed class LevelResource : ScriptableObject
    {
        public ActId act = ActId.Act1;

        [Tooltip("Who the player fights in this level. Must be a Character Resource with the Enemy role.")]
        public CharacterResource enemy;

        [Tooltip("Plays before the battle. Each line says who speaks, so either side can go first.")]
        public List<DialogueLine> startDialogue = new();

        [Tooltip("Plays when the player defeats the enemy. It replaces the enemy's praise on the winning hit.")]
        public List<DialogueLine> victoryDialogue = new();

        [Tooltip("Plays when the enemy defeats the player. It replaces the taunt and solution on the losing hit.")]
        public List<DialogueLine> defeatDialogue = new();

        [Tooltip("Question pools, used in order. Each pool decides which of its questions are asked; tick Override Count to change how many for this level only. Mix fixed and weighted pools, for example 3 easy then 2 moderate.")]
        public List<LevelQuestionPool> questionPools = new();

        [Header("Stars")]
        [Tooltip("Finish within this many seconds of answering time for 3 stars.")]
        [Min(0f)] public float threeStarTime = 60f;

        [Tooltip("Finish within this many seconds of answering time for 2 stars. Slower than this still earns 1 star.")]
        [Min(0f)] public float twoStarTime = 120f;

        private void OnValidate()
        {
            if (enemy != null && enemy.role != CharacterRole.Enemy)
                Debug.LogWarning($"{name}: '{enemy.name}' is not an Enemy character.", this);

            if (twoStarTime < threeStarTime)
                twoStarTime = threeStarTime;
        }

        /// <summary>Stars for finishing this level in the given answering time: 3, 2, or 1 (finishing always earns 1).</summary>
        public int StarsForTime(float seconds)
        {
            if (seconds <= threeStarTime)
                return 3;
            return seconds <= twoStarTime ? 2 : 1;
        }

        /// <summary>
        /// Asks every pool for its picks, in order. Call once when the level starts.
        /// <paramref name="avoid"/> is the texts of the questions asked last time, so a restart gets new ones
        /// whenever the pools have enough. Questions already picked for this level are avoided too, so the same
        /// question never appears twice in one run.
        /// </summary>
        public List<QuestionResource> BuildQuestionList(IEnumerable<string> avoid = null)
        {
            HashSet<string> avoidSet = avoid != null ? new HashSet<string>(avoid) : new HashSet<string>();
            List<QuestionResource> result = new();
            foreach (LevelQuestionPool entry in questionPools)
            {
                if (entry == null || entry.pool == null)
                    continue;

                int before = result.Count;
                entry.pool.Pick(result, entry.overrideCount ? entry.questionCount : -1, avoidSet);

                for (int i = before; i < result.Count; i++)
                    avoidSet.Add(result[i].question);
            }

            return result;
        }
    }
}
