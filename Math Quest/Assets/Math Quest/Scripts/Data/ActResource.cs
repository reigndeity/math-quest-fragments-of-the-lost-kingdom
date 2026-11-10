using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One Act. Levels, enemies, and island art will join this resource later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActResource_Act1",
        menuName = "Math Quest/Resources/Act Resource")]
    public sealed class ActResource : ScriptableObject
    {
        public ActId act = ActId.Act1;

        public string displayName;
    }
}
