using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One level of an Act. For now it owns the dialogue that plays before the battle and
    /// the questions asked in it. Who speaks first is simply the order of the lines.
    /// Enemy and end dialogue will join this resource later.
    /// </summary>
    [CreateAssetMenu(
        fileName = "LevelResource_Act1_1",
        menuName = "Math Quest/Resources/Level Resource")]
    public sealed class LevelResource : ScriptableObject
    {
        public ActId act = ActId.Act1;

        [Tooltip("Plays before the battle. Each line says who speaks, so either side can go first.")]
        public List<DialogueLine> startDialogue = new();

        [Tooltip("The questions of this level, asked in order. The enemy says each one, then the answer panel opens.")]
        public List<QuestionResource> questions = new();
    }
}
