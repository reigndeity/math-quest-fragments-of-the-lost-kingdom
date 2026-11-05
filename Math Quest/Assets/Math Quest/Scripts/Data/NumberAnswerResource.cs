using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>Answer data for a question answered on the number keypad.</summary>
    [CreateAssetMenu(
        fileName = "AnswerResource_Act1_2",
        menuName = "Math Quest/Resources/Number Answer Resource")]
    public sealed class NumberAnswerResource : AnswerResource
    {
        private const double Tolerance = 1e-9;

        [SerializeField] private double correctValue;

        [Tooltip("Other values that also count as correct, for example 0.5 when the answer is 1/2.")]
        [SerializeField] private List<double> alternateValues = new();

        public override AnswerMethod Method => AnswerMethod.NumberKeypad;
        public double CorrectValue => correctValue;

        public bool IsCorrect(double value)
        {
            if (Math.Abs(value - correctValue) < Tolerance)
                return true;

            foreach (double alternate in alternateValues)
            {
                if (Math.Abs(value - alternate) < Tolerance)
                    return true;
            }

            return false;
        }
    }
}
