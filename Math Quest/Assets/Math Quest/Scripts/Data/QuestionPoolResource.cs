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
        /// </summary>
        public abstract void Pick(List<QuestionResource> into, int countOverride = -1);
    }
}
