using System;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Four-button input for multiple-choice questions. Reports the tapped option's index;
    /// it does not decide whether the answer is correct.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MultipleChoiceInput : MonoBehaviour
    {
        [Tooltip("Exactly four buttons, in A to D order. Each needs a TextMeshPro label as a child.")]
        [SerializeField] private Button[] optionButtons = new Button[MultipleChoiceAnswerResource.DefaultOptionCount];

        private Action<int> onSubmitted;

        private void Awake()
        {
            if (optionButtons.Length != MultipleChoiceAnswerResource.DefaultOptionCount)
            {
                Debug.LogError("MultipleChoiceInput needs exactly four option buttons.", this);
                return;
            }

            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.AddListener(() => Submit(index));
            }
        }

        public void Show(MultipleChoiceAnswerResource answer, Action<int> submitted)
        {
            onSubmitted = submitted;

            for (int i = 0; i < optionButtons.Length; i++)
            {
                TMP_Text label = optionButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = answer.GetOption(i);
            }

            SetInteractable(true);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            foreach (Button button in optionButtons)
                button.interactable = interactable;
        }

        private void Submit(int index)
        {
            onSubmitted?.Invoke(index);
        }
    }
}
