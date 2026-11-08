using System;
using UnityEngine;

namespace MathQuest.Data
{
    public enum DialogueSpeaker
    {
        Player = 0,
        Enemy = 1,
    }

    /// <summary>One line of dialogue and who says it.</summary>
    [Serializable]
    public struct DialogueLine
    {
        public DialogueSpeaker speaker;

        [TextArea(1, 3)]
        public string text;
    }
}
