using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One level of an Act. For now it only owns the dialogue that plays before the
    /// battle. Who speaks first is simply the order of the lines. Enemy, questions and
    /// end dialogue will join this resource later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelResource_Act1_1",
        menuName = "Math Quest/Resources/Level Resource")]
    public sealed class LevelResource : ScriptableObject
    {
        public ActId act = ActId.Act1;

        [Tooltip("Plays before the battle. Each line says who speaks, so either side can go first.")]
        public List<DialogueLine> startDialogue = new();
    }
}
