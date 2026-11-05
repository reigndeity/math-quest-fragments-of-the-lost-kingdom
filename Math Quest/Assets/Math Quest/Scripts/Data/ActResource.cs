using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One Act and the questions it draws from. Levels, enemies, and island art will join
    /// this resource later; for now it is the list the battle steps through.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActResource_Act1",
        menuName = "Math Quest/Resources/Act Resource")]
    public sealed class ActResource : ScriptableObject
    {
        public ActId act = ActId.Act1;

        public string displayName;

        [Tooltip("The questions for this Act, in the order the demo battle shows them.")]
        public List<QuestionResource> questions = new();
    }
}
