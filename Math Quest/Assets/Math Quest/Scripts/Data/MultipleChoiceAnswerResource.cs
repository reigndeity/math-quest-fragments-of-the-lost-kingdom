using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MathQuest.Data
{
    /// <summary>
    /// Answer data for a multiple-choice question. Any number of options works (4 by
    /// default); the game may shuffle the display order at runtime, so always compare by
    /// index into <see cref="Options"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AnswerResource_Act1_1",
        menuName = "Math Quest/Resources/Multiple Choice Answer Resource")]
    public sealed class MultipleChoiceAnswerResource : AnswerResource
    {
        public const int DefaultOptionCount = 4;

        [Tooltip("The choices, in authored order. Add or remove entries to change how many are shown.")]
        [SerializeField] private List<string> options = new() { "", "", "", "" };

        [Tooltip("Position of the right answer in Options, starting at 0 (0 = first option).")]
        [FormerlySerializedAs("correctOption")]
        [SerializeField, Min(0)] private int correctIndex;

        public override AnswerMethod Method => AnswerMethod.MultipleChoice;
        public IReadOnlyList<string> Options => options;
        public int CorrectIndex => correctIndex;

        /// <summary>The option text, or an empty string if the index is out of range.</summary>
        public string GetOption(int index)
        {
            return index >= 0 && index < options.Count ? options[index] : string.Empty;
        }

        private void OnValidate()
        {
            if (options.Count > 0)
                correctIndex = Mathf.Clamp(correctIndex, 0, options.Count - 1);
        }
    }
}
