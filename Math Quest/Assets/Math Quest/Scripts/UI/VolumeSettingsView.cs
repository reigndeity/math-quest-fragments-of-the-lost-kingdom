using MathQuest.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// On the SettingHolder (the panel with the sound and music sliders). Shows the saved volumes when the panel
    /// opens, and saves them when a slider moves. The mute icons follow the sliders by themselves, so they come
    /// back as they were left.
    /// </summary>
    public sealed class VolumeSettingsView : MonoBehaviour
    {
        [SerializeField] private Slider soundSlider;
        [SerializeField] private Slider musicSlider;

        private bool loading;

        private void Awake()
        {
            if (soundSlider != null) soundSlider.onValueChanged.AddListener(OnSoundChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        private void OnDestroy()
        {
            if (soundSlider != null) soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        }

        private void OnEnable()
        {
            // Setting .value (not just the number) makes the mute icons update too.
            loading = true;
            if (soundSlider != null) soundSlider.value = Mathf.Lerp(soundSlider.minValue, soundSlider.maxValue, GameSettings.SoundVolume);
            if (musicSlider != null) musicSlider.value = Mathf.Lerp(musicSlider.minValue, musicSlider.maxValue, GameSettings.MusicVolume);
            loading = false;
        }

        private void OnDisable() => GameSettings.Save();

        // On a phone the game can be closed from the app switcher without any other warning.
        private void OnApplicationPause(bool paused)
        {
            if (paused)
                GameSettings.Save();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
                GameSettings.Save();
        }

        private void OnSoundChanged(float value)
        {
            if (!loading && soundSlider != null)
                GameSettings.SoundVolume = Mathf.InverseLerp(soundSlider.minValue, soundSlider.maxValue, value);
        }

        private void OnMusicChanged(float value)
        {
            if (!loading && musicSlider != null)
                GameSettings.MusicVolume = Mathf.InverseLerp(musicSlider.minValue, musicSlider.maxValue, value);
        }
    }
}
