using System.Collections.Generic;
using MathQuest.Data;
using TexDrawLib;
using UnityEngine;
using UnityEngine.Serialization;

namespace MathQuest.Battle
{
    /// <summary>
    /// Demo-only driver for the battle scene: start dialogue, then each question of the Act
    /// in order, with the matching answer input and the encourage or taunt-and-solution
    /// dialogue after each one. After the last question it starts over. No HP yet.
    /// Start dialogue lives here as a stand-in until a level resource owns it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleDemoController : MonoBehaviour
    {
        [Tooltip("The Act whose questions this demo battle cycles through.")]
        [SerializeField] private ActResource act;
        [SerializeField] private DialogueBox dialogueBox;

        [Header("Question UI")]
        [SerializeField] private GameObject questionPanel;
        [Tooltip("TEXDraw label that renders the question, including math such as fractions.")]
        [SerializeField] private TEXDraw questionText;

        [Header("Answer area")]
        [Tooltip("Parent of the answer inputs. Hidden during dialogue, shown while a question waits for an answer.")]
        [SerializeField] private GameObject answerPanel;

        [Header("Answer inputs")]
        [FormerlySerializedAs("answerInput")]
        [SerializeField] private MultipleChoiceInput multipleChoiceInput;
        [SerializeField] private NumberKeypadInput numberKeypadInput;
        [SerializeField] private ExpressionBuilderInput expressionBuilderInput;

        [Header("Demo dialogue (stand-in for the level)")]
        [TextArea(1, 3)]
        [SerializeField] private List<string> startDialogue = new();

        private int questionIndex;

        private void Start()
        {
            questionPanel.SetActive(false);
            HideAnswerUi();
            dialogueBox.Play(startDialogue, ShowQuestion);
        }

        private void ShowQuestion()
        {
            if (act == null || act.questions.Count == 0)
            {
                Debug.LogWarning("BattleDemoController needs an Act with at least one question.", this);
                return;
            }

            QuestionResource question = act.questions[questionIndex];
            if (question == null)
            {
                Debug.LogWarning($"Question {questionIndex + 1} of '{act.name}' is empty.", this);
                return;
            }

            questionText.text = question.question;
            questionPanel.SetActive(true);

            HideAnswerUi();
            if (answerPanel != null) answerPanel.SetActive(true);

            switch (question.answer)
            {
                case MultipleChoiceAnswerResource choice:
                    multipleChoiceInput.Show(choice, index => Resolve(question, index == choice.CorrectIndex));
                    break;
                case NumberAnswerResource number:
                    numberKeypadInput.Show(value => Resolve(question, number.IsCorrect(value)));
                    break;
                case ExpressionBuilderAnswerResource builder:
                    expressionBuilderInput.Show(builder, placed => Resolve(question, builder.IsCorrect(placed)));
                    break;
                default:
                    Debug.LogWarning(
                        $"No input exists yet for the answer method of '{question.name}'.", this);
                    break;
            }
        }

        private void Resolve(QuestionResource question, bool correct)
        {
            HideAnswerUi(); // The answer is in, so the enemy speaks without the inputs on screen.

            if (correct)
            {
                dialogueBox.Play(question.encourageDialogue, NextQuestion);
                return;
            }

            // Taunt is optional and skips itself when empty; the solution always follows.
            dialogueBox.Play(question.tauntDialogue, () =>
                dialogueBox.Play(question.solutionDialogue, NextQuestion));
        }

        // Demo only: right or wrong, move on to the next question and wrap around at the end.
        private void NextQuestion()
        {
            questionIndex = (questionIndex + 1) % act.questions.Count;
            ShowQuestion();
        }

        private void HideAnswerUi()
        {
            if (multipleChoiceInput != null) multipleChoiceInput.Hide();
            if (numberKeypadInput != null) numberKeypadInput.Hide();
            if (expressionBuilderInput != null) expressionBuilderInput.Hide();
            if (answerPanel != null) answerPanel.SetActive(false);
        }
    }
}
