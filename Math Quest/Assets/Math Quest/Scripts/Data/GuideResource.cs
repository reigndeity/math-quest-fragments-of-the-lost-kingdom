using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// A guide character, for example the Magical Guide Book. It is not a fighter: it has no hearts, cannot be
    /// hit and never attacks, so it is its own asset and not a Character Resource.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GuideResource_Guide",
        menuName = "Math Quest/Resources/Guide Resource")]
    public sealed class GuideResource : ScriptableObject
    {
        [Tooltip("Shown as the guide's name, for example Magical Guide Book.")]
        public string displayName = "Guide";

        [Tooltip("The picture shown on the question when this guide is the one asking. Empty: no asker picture is shown.")]
        public Sprite askerSprite;

        [Tooltip("The guide's everyday picture, the one shown on screen when it appears.")]
        public Sprite defaultSprite;

        [Header("Practice (after a wrong answer, the guide asks similar questions)")]
        [Tooltip("The most practice questions the guide asks in a row. It stops early on the first right answer. 0 turns practice off.")]
        [Min(0)] public int maxPracticeQuestions = 5;

        [Tooltip("Extra pools to find similar questions in. The level's own pools are always searched too.")]
        public List<QuestionPoolResource> practicePools = new();

        [Tooltip("Said before the first practice question. One is picked at random.")]
        [TextArea(1, 3)] public List<string> practiceIntroLines = new() { "Let's try a similar one!" };

        [Tooltip("Said before the next practice question after a wrong one. One is picked at random.")]
        [TextArea(1, 3)] public List<string> retryIntroLines = new() { "Let's try another one!" };

        [Tooltip("Said after a right practice answer. One is picked at random.")]
        [TextArea(1, 3)] public List<string> praiseLines = new() { "Well done! You got it!" };

        [Header("When the wrong answer took the player's last heart")]
        [Tooltip("Said after the player got a practice question right, just before the level's defeat dialogue. One is picked at random.")]
        [TextArea(1, 3)] public List<string> tryAgainPraiseLines = new() { "Great job, you've got it now! Let's give it another try!" };

        [Tooltip("Said instead when the player never got a practice question right. One is picked at random.")]
        [TextArea(1, 3)] public List<string> tryAgainLines = new() { "Don't give up! Look at it again, and let's try once more!" };
    }
}
