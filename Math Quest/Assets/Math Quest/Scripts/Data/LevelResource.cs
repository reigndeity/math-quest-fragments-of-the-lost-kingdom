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

        [Tooltip("Plays before the battle. Each line says who speaks, so either side can go first.")]
        public List<DialogueLine> startDialogue = new();

        [Tooltip("Question pools, used in order. Each pool decides which of its questions are asked; tick Override Count to change how many for this level only. Mix fixed and weighted pools, for example 3 easy then 2 moderate.")]
        public List<LevelQuestionPool> questionPools = new();

        /// <summary>Asks every pool for its picks, in order. Call once when the level starts.</summary>
        public List<QuestionResource> BuildQuestionList()
        {
            List<QuestionResource> result = new();
            foreach (LevelQuestionPool entry in questionPools)
            {
                if (entry != null && entry.pool != null)
                    entry.pool.Pick(result, entry.overrideCount ? entry.questionCount : -1);
            }

            return result;
        }
    }
}
