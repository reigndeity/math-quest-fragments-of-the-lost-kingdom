using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// The KeypadHolder prefab: a calculator-style number keypad. The display shows ? until
    /// something is typed; digits replace a lone 0, the sign key flips the sign at any time,
    /// C clears and DEL removes the last character. There is no submit key of its own: the
    /// battle's one SubmitButton hands the answer in. It does not decide whether the answer
    /// is correct. Key labels live in the prefab, so restyle them there.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NumberKeypadPanel : MonoBehaviour
    {
        private const int MaxDigits = 8;
        private const string Zero = "0";
        private const string EmptyDisplay = "?";

        [Tooltip("Exactly ten buttons, in order 0 to 9.")]
        [SerializeField] private Button[] digitButtons = new Button[10];
        [SerializeField] private Button signButton;
        [SerializeField] private Button decimalButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private TMP_Text display;

        private CanvasGroup group;
        private Animator animator;
        private Action changed;
        private bool locked;
        private bool hasInput;
        private bool negative;
        private string magnitude = Zero;

        /// <summary>True once the player has typed something.</summary>
        public bool HasValue => hasInput;

        /// <summary>What is typed, for example -15, or empty if nothing is typed yet.</summary>
        public string DisplayText => hasInput ? (negative ? "-" : string.Empty) + magnitude : string.Empty;

        public bool TryGetValue(out double value)
        {
            value = 0;
            if (!hasInput)
                return false;

            if (!double.TryParse(magnitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                return false;

            value = negative ? -parsed : parsed;
            return true;
        }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null)
                group = gameObject.AddComponent<CanvasGroup>();
            animator = GetComponent<Animator>();

            if (digitButtons.Length != 10)
            {
                Debug.LogError("NumberKeypadPanel needs exactly ten digit buttons.", this);
                return;
            }

            for (int i = 0; i < digitButtons.Length; i++)
            {
                string digit = i.ToString();
                digitButtons[i].onClick.AddListener(() => AppendDigit(digit));
            }

            signButton.onClick.AddListener(ToggleSign);
            decimalButton.onClick.AddListener(AppendDecimal);
            clearButton.onClick.AddListener(Clear);
            deleteButton.onClick.AddListener(Delete);
        }

        /// <summary>Resets the keypad. <paramref name="onChanged"/> fires after every key press.</summary>
        public void Show(Action onChanged)
        {
            changed = onChanged;
            locked = false;
            ResetInput();
            Refresh();
        }

        /// <summary>
        /// Hides the keypad without taking it out of the layout, and stops it taking input. If
        /// the prefab has an Animator its intro animation is held back until Reveal().
        /// </summary>
        public void Conceal()
        {
            if (animator != null)
                animator.enabled = false;

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        /// <summary>
        /// Lets the keypad take input and shows it: by playing its Animator's intro animation if
        /// it has one, otherwise by fading in over <paramref name="fadeDuration"/>.
        /// </summary>
        public void Reveal(float fadeDuration)
        {
            group.interactable = true;
            group.blocksRaycasts = true;

            if (animator != null)
                animator.enabled = true; // starts its state from the beginning
            else
                StartCoroutine(FadeIn(fadeDuration));
        }

        /// <summary>Stops the keys reacting, for example once the answer is submitted.</summary>
        public void Lock()
        {
            locked = true;
            group.interactable = false;
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                yield return null;
            }

            group.alpha = 1f;
        }

        private void AppendDigit(string digit)
        {
            if (locked)
                return;

            hasInput = true;

            if (magnitude == Zero)
                magnitude = digit; // A lone 0 is replaced, so 0 then 7 reads 7, never 07.
            else if (CountDigits() < MaxDigits)
                magnitude += digit;

            Changed();
        }

        private void AppendDecimal()
        {
            if (locked)
                return;

            hasInput = true;

            if (!magnitude.Contains('.'))
                magnitude += ".";

            Changed();
        }

        private void ToggleSign()
        {
            if (locked)
                return;

            hasInput = true;
            negative = !negative;
            Changed();
        }

        private void Clear()
        {
            if (locked)
                return;

            ResetInput();
            Changed();
        }

        private void Delete()
        {
            if (locked)
                return;

            if (magnitude.Length > 1)
            {
                magnitude = magnitude.Substring(0, magnitude.Length - 1);
                Changed();
                return;
            }

            ResetInput(); // Nothing left to delete, so back to the empty ? display.
            Changed();
        }

        private void ResetInput()
        {
            hasInput = false;
            negative = false;
            magnitude = Zero;
        }

        private int CountDigits()
        {
            int count = 0;
            foreach (char character in magnitude)
            {
                if (char.IsDigit(character))
                    count++;
            }

            return count;
        }

        private void Changed()
        {
            Refresh();
            changed?.Invoke();
        }

        private void Refresh()
        {
            display.text = hasInput ? DisplayText : EmptyDisplay;
        }
    }
}
