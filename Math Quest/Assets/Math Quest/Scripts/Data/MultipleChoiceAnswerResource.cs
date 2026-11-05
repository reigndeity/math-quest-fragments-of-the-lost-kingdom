using System;
using UnityEngine;

namespace MathQuest.Data
{
    public enum OptionLetter
    {
        A = 0,
        B = 1,
        C = 2,
        D = 3
    }

    /// <summary>
    /// Answer data for a multiple-choice question. The authored order is A to D;
    /// the game may shuffle the display order at runtime, so always compare by index.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AnswerResource_Act1_1",
        menuName = "Math Quest/Resources/Multiple Choice Answer Resource")]
    public sealed class MultipleChoiceAnswerResource : AnswerResource
    {
        public const int OptionCount = 4;

        [SerializeField] private string optionA;
        [SerializeField] private string optionB;
        [SerializeField] private string optionC;
        [SerializeField] private string optionD;
        [SerializeField] private OptionLetter correctOption;

        public override AnswerMethod Method => AnswerMethod.MultipleChoice;
        public OptionLetter CorrectOption => correctOption;
        public int CorrectIndex => (int)correctOption;

        public string GetOption(int index)
        {
            return index switch
            {
                0 => optionA,
                1 => optionB,
                2 => optionC,
                3 => optionD,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
    }
}
