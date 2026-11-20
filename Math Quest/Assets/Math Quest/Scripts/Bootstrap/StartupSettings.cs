using UnityEngine;

namespace MathQuest.Bootstrap
{
    /// <summary>
    /// Settings that apply to the whole game and run once when it starts, before any scene loads (so it does not
    /// matter which scene is played first).
    /// </summary>
    public static class StartupSettings
    {
        /// <summary>How many frames a second the game aims for. Android would otherwise default to 30.</summary>
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            // Only a target: a phone that cannot keep up simply runs slower. VSync is off in the quality settings,
            // so this value is the one that counts.
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
