using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MathQuest.Data
{
    [Serializable]
    public sealed class WeightedQuestion
    {
        public QuestionResource question;

        [Tooltip("Higher weight, more likely to be drawn. 0 means never.")]
        [Min(0f)] public float weight = 1f;
    }

    /// <summary>
    /// Draws a number of questions at random, favouring higher weights. Each question can
    /// be drawn once per level unless Allow Repeats is on.
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuestionPool_Weighted",
        menuName = "Math Quest/Resources/Question Pool (Weighted)")]
    public sealed class QuestionWeightedPoolResource : QuestionPoolResource
    {
        [SerializeField] private List<WeightedQuestion> entries = new();

        [Tooltip("How many questions this pool contributes to the level.")]
        [SerializeField, Min(1)] private int drawCount = 3;

        [Tooltip("Off: a question can only appear once. On: the same question can be drawn again.")]
        [SerializeField] private bool allowRepeats;

        public override void Pick(List<QuestionResource> into, int countOverride = -1)
        {
            List<WeightedQuestion> candidates = new();
            foreach (WeightedQuestion entry in entries)
            {
                if (entry != null && entry.question != null && entry.weight > 0f)
                    candidates.Add(entry);
            }

            int count = countOverride >= 0 ? countOverride : drawCount;
            for (int n = 0; n < count && candidates.Count > 0; n++)
            {
                float total = 0f;
                foreach (WeightedQuestion entry in candidates)
                    total += entry.weight;

                float roll = Random.value * total;
                int chosen = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    roll -= candidates[i].weight;
                    if (roll <= 0f)
                    {
                        chosen = i;
                        break;
                    }
                }

                into.Add(candidates[chosen].question);
                if (!allowRepeats)
                    candidates.RemoveAt(chosen);
            }
        }
    }
}
