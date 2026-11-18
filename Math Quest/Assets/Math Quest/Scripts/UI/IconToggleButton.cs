using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// Put on a Button: shows an "on" or "off" icon. Used for the sound and music mute buttons.
    /// When a slider is assigned the two stay in sync:
    /// slider above 0 = on, slider at 0 = off. Clicking the button mutes (slider to 0)
    /// or restores the last non-zero volume. Audio hooks into <see cref="OnToggled"/> later.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class IconToggleButton : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField] private bool isOn = true;
        [Tooltip("Optional. When set, the slider drives the icon and the button drives the slider.")]
        [SerializeField] private Slider slider;

        /// <summary>Raised with the new state (true = on / not muted).</summary>
        public UnityEvent<bool> OnToggled = new UnityEvent<bool>();

        private Button button;
        private float lastVolume = 1f;

        public bool IsOn => isOn;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Toggle);

            if (slider != null)
            {
                if (slider.value > 0f) lastVolume = slider.value;
                isOn = slider.value > 0f;
                slider.onValueChanged.AddListener(OnSliderChanged);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(Toggle);
            if (slider != null) slider.onValueChanged.RemoveListener(OnSliderChanged);
        }

        public void Toggle() => SetOn(!isOn);

        public void SetOn(bool value)
        {
            if (slider != null)
            {
                if (!value && slider.value > 0f) lastVolume = slider.value;
                // The slider's change callback updates the icon and raises OnToggled.
                slider.value = value ? (lastVolume > 0f ? lastVolume : slider.maxValue) : slider.minValue;
                return;
            }

            Apply(value);
        }

        private void OnSliderChanged(float value)
        {
            if (value > 0f) lastVolume = value;
            Apply(value > 0f);
        }

        private void Apply(bool value)
        {
            bool changed = isOn != value;
            isOn = value;
            Refresh();
            if (changed) OnToggled.Invoke(isOn);
        }

        private void Refresh()
        {
            if (icon != null) icon.sprite = isOn ? onSprite : offSprite;
        }

#if UNITY_EDITOR
        private void OnValidate() => Refresh();
#endif
    }
}
