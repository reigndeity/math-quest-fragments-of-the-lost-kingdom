using UnityEngine;

namespace MathQuest.Data
{
    public enum AnswerMethod
    {
        MultipleChoice = 0,
        NumberKeypad = 1,
        ExpressionBuilder = 2
    }

    /// <summary>
    /// Base for every kind of answer. A question references one; the battle reads
    /// <see cref="Method"/> to decide which input to show.
    /// </summary>
    public abstract class AnswerResource : ScriptableObject
    {
        public abstract AnswerMethod Method { get; }
    }
}
