using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// A bag of questions a level draws from. Same idea as the loot table pools in KOM:
    /// a level holds a list of pools, and each pool decides which of its questions are used.
    /// </summary>
    public abstract class QuestionPoolResource : ScriptableObject
    {
        /// <summary>
        /// Appends this pool's picks, in the order they will be asked. A negative
        /// <paramref name="countOverride"/> uses the pool's own count; otherwise the level's
        /// number is used instead.
        /// <paramref name="avoid"/> holds question texts to skip when the pool has other questions to use
        /// (the ones asked last time, and the ones already picked for this level). Only when the pool runs out of
        /// fresh questions does it fall back to the avoided ones.
        /// </summary>
        public abstract void Pick(List<QuestionResource> into, int countOverride = -1, ICollection<string> avoid = null);

        /// <summary>Appends every question this pool can ever give (not a pick), for example to find similar ones.</summary>
        public abstract void CollectAll(List<QuestionResource> into);
    }
}
