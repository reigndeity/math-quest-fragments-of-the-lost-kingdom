using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Calculator-style number keypad. The display shows ? until something is typed; digits
    /// replace a lone 0, the sign key flips the sign of the displayed number at any time,
    /// C clears, DEL removes the last character. Reports the value on submit and does not
    /// decide whether it is correct. Button labels are set by this script, so the buttons
    /// only need a TextMeshPro label as a child.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NumberKeypadInput : MonoBehaviour
    {
        private const int MaxDigits = 8;
        private const string Zero = "0";
        private const string EmptyDisplay = "?";

        [Tooltip("Exactly ten buttons, in order 0 to 9.")]
        [SerializeField] private Button[] digitButtons = new Button[10];
        [FormerlySerializedAs("minusButton")]
        [SerializeField] private Button signButton;
        [SerializeField] private Button decimalButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_Text display;

        private bool hasInput;
        private bool negative;
        private string magnitude = Zero;
        private Action<double> onSubmitted;

        private void Awake()
        {
            if (digitButtons.Length != 10)
            {
                Debug.LogError("NumberKeypadInput needs exactly ten digit buttons.", this);
                return;
            }

            for (int i = 0; i < digitButtons.Length; i++)
            {
                string digit = i.ToString();
                SetLabel(digitButtons[i], digit);
                digitButtons[i].onClick.AddListener(() => AppendDigit(digit));
            }

            SetLabel(signButton, "+/-");
            signButton.onClick.AddListener(ToggleSign);

            SetLabel(decimalButton, ".");
            decimalButton.onClick.AddListener(AppendDecimal);

            SetLabel(clearButton, "C");
            clearButton.onClick.AddListener(Clear);

            SetLabel(deleteButton, "DEL");
            deleteButton.onClick.AddListener(Delete);

            SetLabel(submitButton, "SUBMIT");
            submitButton.onClick.AddListener(Submit);
        }

        public void Show(Action<double> submitted)
        {
            onSubmitted = submitted;
            Clear();
            SetInteractable(true);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            foreach (Button button in digitButtons)
                button.interactable = interactable;

            signButton.interactable = interactable;
            decimalButton.interactable = interactable;
            clearButton.interactable = interactable;
            deleteButton.interactable = interactable;
            submitButton.interactable = interactable;
        }

        private void AppendDigit(string digit)
        {
            hasInput = true;

            if (magnitude == Zero)
            {
                magnitude = digit; // A lone 0 is replaced, so 0 then 7 reads 7, never 07.
            }
            else if (CountDigits() < MaxDigits)
            {
                magnitude += digit;
            }

            Refresh();
        }

        private void AppendDecimal()
        {
            hasInput = true;

            if (!magnitude.Contains('.'))
                magnitude += ".";

            Refresh();
        }

        private void ToggleSign()
        {
            hasInput = true;
            negative = !negative;
            Refresh();
        }

        private void Clear()
        {
            hasInput = false;
            negative = false;
            magnitude = Zero;
            Refresh();
        }

        private void Delete()
        {
            if (magnitude.Length > 1)
            {
                magnitude = magnitude.Substring(0, magnitude.Length - 1);
                Refresh();
                return;
            }

            Clear(); // Nothing left to delete, so back to the empty ? display.
        }

        private void Submit()
        {
            if (!hasInput)
                return; // Nothing typed yet.

            if (!double.TryParse(
                    magnitude,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double value))
            {
                return;
            }

            onSubmitted?.Invoke(negative ? -value : value);
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

        private void Refresh()
        {
            display.text = hasInput
                ? (negative ? "-" : string.Empty) + magnitude
                : EmptyDisplay;
        }

        private static void SetLabel(Button button, string text)
        {
            button.GetComponentInChildren<TMP_Text>().text = text;
        }
    }
}
