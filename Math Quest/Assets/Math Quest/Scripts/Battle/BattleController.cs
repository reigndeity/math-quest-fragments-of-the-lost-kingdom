using System.Collections;
using MathQuest.Data;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// Drives a battle from a LevelResource: start dialogue, then each question. The enemy
    /// says the question; once it has typed, the screen blurs, the answer panel opens and
    /// the answer input is spawned. Picking an answer closes the panel again.
    /// Reacting to the answer (player line, praise, taunt, solution) comes next.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private LevelResource level;
        [SerializeField] private BattleDialogueController dialogue;
        [SerializeField] private BlurBackdropController blur;

        [Header("Answer panel")]
        [Tooltip("The AnswerPanel object. Starts hidden and is shown while a question waits for an answer.")]
        [SerializeField] private GameObject answerPanel;
        [Tooltip("The QuestionBoxFitter on QuestionBoxHolder. Repeats the question and sizes the box to fit it.")]
        [SerializeField] private QuestionBoxFitter questionBox;
        [Tooltip("The AnswerArea inside the panel. Answer inputs are spawned here.")]
        [SerializeField] private RectTransform answerArea;

        [Header("Timing")]
        [Tooltip("Seconds to wait after the question has finished typing, so it can be read, before the panel opens.")]
        [SerializeField, Min(0f)] private float readDelay = 0.8f;

        [Header("Answer inputs")]
        [SerializeField] private MultipleChoicePanel multipleChoicePrefab;

        private int questionIndex;
        private GameObject answerUi;

        private void Start()
        {
            HidePanelContents();
            answerPanel.SetActive(false);
            dialogue.PlayLines(level.startDialogue, AskQuestion);
        }

        private void AskQuestion()
        {
            if (level.questions.Count == 0 || level.questions[questionIndex] == null)
            {
                Debug.LogWarning("The level has no question to ask.", this);
                return;
            }

            QuestionResource question = level.questions[questionIndex];
            dialogue.SayAndHold(
                DialogueSpeaker.Enemy,
                question.question,
                () => StartCoroutine(OpenAnswerPanel(question)));
        }

        private IEnumerator OpenAnswerPanel(QuestionResource question)
        {
            // The question has finished typing; give the player a moment to read it.
            yield return new WaitForSeconds(readDelay);

            // The snapshot must be taken while the panel is still hidden.
            yield return blur.Capture();

            // The panel must be active before the question is fitted, so the text can be measured.
            answerArea.gameObject.SetActive(true);
            answerPanel.SetActive(true);
            questionBox.Show(question.question);

            SpawnAnswerInput(question);
            blur.FadeIn();
        }

        private void SpawnAnswerInput(QuestionResource question)
        {
            switch (question.answer)
            {
                case MultipleChoiceAnswerResource choice:
                    MultipleChoicePanel panel = Instantiate(multipleChoicePrefab, answerArea, false);
                    answerUi = panel.gameObject;
                    panel.Show(choice, index => OnAnswered(index == choice.CorrectIndex));
                    break;

                default:
                    Debug.LogWarning(
                        $"No answer input exists yet for '{question.name}' ({question.answer}).", this);
                    break;
            }
        }

        private void OnAnswered(bool correct)
        {
            Debug.Log(correct ? "Correct answer." : "Wrong answer.");
            StartCoroutine(CloseAnswerPanel());
        }

        private IEnumerator CloseAnswerPanel()
        {
            // The inputs and the enemy's bubble go at once; only the blur fades out.
            dialogue.ClearBubble();
            HidePanelContents();
            blur.FadeOut();
            yield return new WaitForSeconds(blur.FadeDuration);
            answerPanel.SetActive(false);

            // Temporary, until the reaction dialogue exists: just ask the next question.
            questionIndex = (questionIndex + 1) % level.questions.Count;
            AskQuestion();
        }

        private void HidePanelContents()
        {
            if (answerUi != null)
                Destroy(answerUi);

            answerUi = null;
            questionBox.Hide();
            answerArea.gameObject.SetActive(false);
        }
    }
}
