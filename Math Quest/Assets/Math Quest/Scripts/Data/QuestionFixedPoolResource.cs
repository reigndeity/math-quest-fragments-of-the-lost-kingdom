using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    public enum FixedPoolStrategy
    {
        /// <summary>Every question in the pool is asked.</summary>
        All = 0,
        /// <summary>Only the first question is asked.</summary>
        First = 1,
        /// <summary>Only the last question is asked.</summary>
        Last = 2
    }

    /// <summary>
    /// A hand-picked list. With Strategy All every question is asked, in list order
    /// (or shuffled if Shuffle is on).
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuestionPool_Fixed",
        menuName = "Math Quest/Resources/Question Pool (Fixed)")]
    public sealed class QuestionFixedPoolResource : QuestionPoolResource
    {
        [SerializeField] private List<QuestionResource> questions = new();

        [SerializeField] private FixedPoolStrategy strategy = FixedPoolStrategy.All;

        [Tooltip("Shuffle the order of the picked questions each time the level starts. Only matters with Strategy All.")]
        [SerializeField] private bool shuffle;

        public override void Pick(List<QuestionResource> into, int countOverride = -1)
        {
            List<QuestionResource> valid = new();
            foreach (QuestionResource question in questions)
            {
                if (question != null)
                    valid.Add(question);
            }

            if (valid.Count == 0)
                return;

            switch (strategy)
            {
                case FixedPoolStrategy.First:
                    into.Add(valid[0]);
                    return;
                case FixedPoolStrategy.Last:
                    into.Add(valid[valid.Count - 1]);
                    return;
            }

            if (shuffle)
            {
                for (int i = valid.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (valid[i], valid[j]) = (valid[j], valid[i]);
                }
            }

            if (countOverride >= 0 && countOverride < valid.Count)
                valid.RemoveRange(countOverride, valid.Count - countOverride); // keep the first N

            into.AddRange(valid);
        }
    }
}
