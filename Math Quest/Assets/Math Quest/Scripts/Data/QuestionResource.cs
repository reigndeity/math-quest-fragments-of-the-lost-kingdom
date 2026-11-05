using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One question: what is asked, how it is answered, and what the enemy says about it.
    /// Start and end dialogue are not here; they belong to the fight, not the question.
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuestionResource_Act1_1",
        menuName = "Math Quest/Resources/Question Resource")]
    public sealed class QuestionResource : ScriptableObject
    {
        [Header("Identity")]
        public ActId act = ActId.Act1;

        [Tooltip("One of the Act's topics, for example Components of Algebraic Expressions.")]
        public string topic;

        [Header("Question")]
        [TextArea(2, 5)] public string question;

        [Tooltip("How this question is answered. The answer asset's type decides which input is shown.")]
        public AnswerResource answer;

        [Header("Dialogue (optional lines are simply skipped when empty)")]
        [Tooltip("Plays after a correct answer. Optional.")]
        [TextArea(1, 3)] public List<string> encourageDialogue = new();

        [Tooltip("Plays after a wrong answer, before the solution. Optional.")]
        [TextArea(1, 3)] public List<string> tauntDialogue = new();

        [Tooltip("Plays after a wrong answer (after the taunt, if any). Always expected.")]
        [TextArea(1, 5)] public List<string> solutionDialogue = new();
    }
}
