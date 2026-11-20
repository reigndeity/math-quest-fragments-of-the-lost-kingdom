using System;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// The player's settings: how loud the sound and the music are (0 is silent, 1 is full). They are kept in
    /// PlayerPrefs, apart from the saved game, so erasing the game data does not touch them.
    ///
    /// Nothing plays through these yet. When the audio is added, read <see cref="SoundVolume"/> and
    /// <see cref="MusicVolume"/> (for example to set an AudioMixer) and listen to <see cref="Changed"/> to follow
    /// the sliders live.
    /// </summary>
    public static class GameSettings
    {
        private const string SoundKey = "MathQuest.SoundVolume";
        private const string MusicKey = "MathQuest.MusicVolume";

        /// <summary>Raised whenever a volume changes.</summary>
        public static event Action Changed;

        public static float SoundVolume
        {
            get => Read(SoundKey);
            set => Write(SoundKey, value);
        }

        public static float MusicVolume
        {
            get => Read(MusicKey);
            set => Write(MusicKey, value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Changed = null;

        /// <summary>Writes the settings to disk. Call it when a settings screen closes and when the app is paused.</summary>
        public static void Save() => PlayerPrefs.Save();

        // A volume that was never set is full.
        private static float Read(string key) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f));

        private static void Write(string key, float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(Read(key), value))
                return;

            // Kept in memory while a slider is dragged; Save() writes it to disk once the screen closes.
            PlayerPrefs.SetFloat(key, value);
            Changed?.Invoke();
        }
    }
}
