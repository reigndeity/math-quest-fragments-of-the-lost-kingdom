using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
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

        [Tooltip("Question pools, used in order. Each pool decides which of its questions are asked. Mix fixed and weighted pools, for example 3 easy then 2 moderate.")]
        public List<QuestionPoolResource> questionPools = new();

        /// <summary>Asks every pool for its picks, in order. Call once when the level starts.</summary>
        public List<QuestionResource> BuildQuestionList()
        {
            List<QuestionResource> result = new();
            foreach (QuestionPoolResource pool in questionPools)
            {
                if (pool != null)
                    pool.Pick(result);
            }

            return result;
        }
    }
}
