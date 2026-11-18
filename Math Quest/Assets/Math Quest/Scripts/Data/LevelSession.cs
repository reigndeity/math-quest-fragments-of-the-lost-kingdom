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
    }
}
