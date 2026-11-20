using System.Collections.Generic;

namespace MathQuest.Data
{
    /// <summary>
    /// The level the player picked on the level map, carried over into the Battle scene.
    /// Empty when the Battle scene is opened directly, so it falls back to its own Inspector values.
    /// </summary>
    public static class LevelSession
    {
        public static ActResource Act { get; private set; }
        public static LevelResource Level { get; private set; }

        public static bool HasSelection => Act != null && Level != null;

        public static void Set(ActResource act, LevelResource level)
        {
            Act = act;
            Level = level;
        }

        public static void Clear()
        {
            Act = null;
            Level = null;
        }

        // The questions each level asked last time (their texts), so a restart or a replay gets new ones.
        // Kept for as long as the game runs; it does not need to survive closing the game.
        private static readonly Dictionary<LevelResource, List<string>> LastQuestions = new();

        /// <summary>Texts of the questions this level asked last time, or null if it has not been played yet.</summary>
        public static IEnumerable<string> GetLastQuestions(LevelResource level)
        {
            return level != null && LastQuestions.TryGetValue(level, out List<string> texts) ? texts : null;
        }

        public static void RememberQuestions(LevelResource level, IEnumerable<QuestionResource> questions)
        {
            if (level == null || questions == null)
                return;

            List<string> texts = new();
            foreach (QuestionResource question in questions)
            {
                if (question != null)
                    texts.Add(question.question);
            }

            LastQuestions[level] = texts;
        }
    }
}
