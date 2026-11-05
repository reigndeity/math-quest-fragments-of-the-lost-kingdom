using System.Collections.Generic;
using MathQuest.Data;
using TexDrawLib;
using UnityEngine;
using UnityEngine.Serialization;

namespace MathQuest.Battle
{
    /// <summary>
    /// Demo-only driver for the battle scene: start dialogue, the question, the matching
    /// answer input, then the encourage or taunt-and-solution dialogue. No HP yet.
    /// Start dialogue lives here as a stand-in until a level resource owns it.
    /// To try another question, assign a different Question asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleDemoController : MonoBehaviour
    {
        [SerializeField] private QuestionResource question;
        [SerializeField] private DialogueBox dialogueBox;

        [Header("Question UI")]
        [SerializeField] private GameObject questionPanel;
        [Tooltip("TEXDraw label that renders the question, including math such as fractions.")]
        [SerializeField] private TEXDraw questionText;

        [Header("Answer inputs")]
        [FormerlySerializedAs("answerInput")]
        [SerializeField] private MultipleChoiceInput multipleChoiceInput;
        [SerializeField] private NumberKeypadInput numberKeypadInput;

        [Header("Demo dialogue (stand-in for the level)")]
        [TextArea(1, 3)]
        [SerializeField] private List<string> startDialogue = new();

        private void Start()
        {
            questionPanel.SetActive(false);
            dialogueBox.Play(startDialogue, ShowQuestion);
        }

        private void ShowQuestion()
        {
            questionText.text = question.question;
            questionPanel.SetActive(true);

            if (multipleChoiceInput != null) multipleChoiceInput.Hide();
            if (numberKeypadInput != null) numberKeypadInput.Hide();

            switch (question.answer)
            {
                case MultipleChoiceAnswerResource choice:
                    multipleChoiceInput.Show(choice, index => Resolve(index == choice.CorrectIndex));
                    break;
                case NumberAnswerResource number:
                    numberKeypadInput.Show(value => Resolve(number.IsCorrect(value)));
                    break;
                default:
                    Debug.LogWarning(
                        $"No input exists yet for the answer method of '{question.name}'.", this);
                    break;
            }
        }

        private void Resolve(bool correct)
        {
            SetInputsInteractable(false);

            if (correct)
            {
                dialogueBox.Play(question.encourageDialogue, ReopenForDemo);
                return;
            }

            // Taunt is optional and skips itself when empty; the solution always follows.
            dialogueBox.Play(question.tauntDialogue, () =>
                dialogueBox.Play(question.solutionDialogue, ReopenForDemo));
        }

        // Demo only: let the tester try the same question again.
        private void ReopenForDemo()
        {
            SetInputsInteractable(true);
        }

        private void SetInputsInteractable(bool interactable)
        {
            if (multipleChoiceInput != null) multipleChoiceInput.SetInteractable(interactable);
            if (numberKeypadInput != null) numberKeypadInput.SetInteractable(interactable);
        }
    }
}
