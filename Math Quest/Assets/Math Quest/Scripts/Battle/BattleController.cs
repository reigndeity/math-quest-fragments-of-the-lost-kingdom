using System;
using System.Collections;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Drives a battle from a LevelResource: start dialogue, then each question. The enemy
    /// says the question; once it has typed, the screen blurs and the question and answers
    /// fade and pop in, one step after the other. Submitting closes it again; the player then
    /// says their answer and the enemy reacts (praise, or taunt and solution). Hearts and the
    /// hurt animation come next.
    ///
    /// Opening, in order (all times are the Timing fields below):
    ///   1. Everything is prepared invisibly: the question box is fitted, the options are
    ///      spawned hidden and the answer area is sized for all of them, so nothing re-lays out.
    ///   2. The blur fades in.
    ///   3. After Holder Fade Delay, QuestionAndAnswerHolder fades from 0 to 1.
    ///   4. After Question Box Delay, QuestionBoxHolder is activated and plays its animation.
    ///   5. The answer input reveals itself. For multiple choice (see its own timing group): after
    ///      Options Delay the options pop in one by one, Option Interval apart.
    ///   6. When the input has finished revealing, Submit Delay later the SubmitButton is
    ///      activated, which plays its own animation.
    ///
    /// The steps up to 4 are common to every question type. Step 5 and the Submit Delay belong to
    /// the answer input, so each type (multiple choice, keypad and expression builder) has its own
    /// timing group.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleController : MonoBehaviour
    {
        [SerializeField] private LevelResource level;
        [Tooltip("Shows the player and the level's enemy (names, hearts, pictures). Optional while setting up.")]
        [SerializeField] private CharacterAssetConfig characters;
        [SerializeField] private BattleDialogueController dialogue;
        [SerializeField] private BlurBackdropController blur;

        [Header("Answer panel (AnswerPanel itself stays active; only its contents come and go)")]
        [Tooltip("The CanvasGroup on QuestionAndAnswerHolder. Faded in and out as a whole.")]
        [SerializeField] private CanvasGroup questionAndAnswer;
        [Tooltip("The QuestionBoxFitter on QuestionBoxHolder. Repeats the question and sizes the box to fit it.")]
        [SerializeField] private QuestionBoxFitter questionBox;
        [Tooltip("The AnswerArea inside the panel. Answer inputs are spawned here.")]
        [SerializeField] private RectTransform answerArea;
        [Tooltip("The AnswerAreaFitter on AnswerAndSubmitHolder. Sizes the answer area to its options and moves the submit area to match. Optional.")]
        [SerializeField] private AnswerAreaFitter answerFitter;

        [Header("Submit")]
        [Tooltip("The one submit button used by every answer input. Only interactable while the player has an answer to submit.")]
        [SerializeField] private Button submitButton;
        [Tooltip("The SubmitArea that holds the button. Shown only while a question waits for an answer.")]
        [SerializeField] private GameObject submitArea;

        [Header("Timing: before the question (seconds)")]
        [Tooltip("Pause before the start dialogue begins.")]
        [SerializeField, Min(0f)] private float startDelay = 0.3f;
        [Tooltip("After the enemy's question has finished typing, how long it can be read before the blur starts.")]
        [SerializeField, Min(0f)] private float readDelay = 0.8f;

        [Header("Timing: opening the panel, same for every question type (seconds)")]
        [Tooltip("After the blur starts, wait this long before QuestionAndAnswerHolder starts fading in.")]
        [SerializeField, Min(0f)] private float holderFadeDelay = 0.2f;
        [Tooltip("How long QuestionAndAnswerHolder takes to fade from 0 to 1.")]
        [SerializeField, Min(0.01f)] private float holderFadeDuration = 0.3f;
        [Tooltip("After the holder starts fading in, wait this long before QuestionBoxHolder is activated (its own animation plays then).")]
        [SerializeField, Min(0f)] private float questionBoxDelay = 0.15f;
        [Tooltip("How long QuestionBoxHolder's own animation takes. The answer area stays put during it instead of sliding along with the question box.")]
        [SerializeField, Min(0f)] private float questionBoxAnimationTime = 0.4f;

        // Timing of the multiple choice input. The keypad and the expression builder each have a
        // group like this of their own, since their entrances differ.
        [Serializable]
        private sealed class MultipleChoiceTiming
        {
            [Tooltip("After QuestionBoxHolder is activated, wait this long before the first option pops in.")]
            [Min(0f)] public float optionsDelay = 0.3f;
            [Tooltip("Time between one option popping in and the next.")]
            [Min(0f)] public float optionInterval = 0.1f;
            [Tooltip("After the last option has popped in, wait this long before the SubmitButton is activated.")]
            [Min(0f)] public float submitDelay = 0.15f;
        }

        [Header("Timing: multiple choice (seconds)")]
        [SerializeField] private MultipleChoiceTiming multipleChoice = new();

        // Timing of the number keypad: its intro plays as a whole, then the submit button follows.
        [Serializable]
        private sealed class KeypadTiming
        {
            [Tooltip("After QuestionBoxHolder is activated, wait this long before the keypad starts fading in.")]
            [Min(0f)] public float keypadDelay = 0.3f;
            [Tooltip("How long the keypad's intro takes. Set it to the length of the KeypadHolder animation. If the prefab has no Animator, the keypad fades in over this time instead.")]
            [Min(0.01f)] public float keypadAnimationTime = 0.35f;
            [Tooltip("After the keypad's intro has finished, wait this long before the SubmitButton is activated.")]
            [Min(0f)] public float submitDelay = 0.15f;
        }

        [Header("Timing: number keypad (seconds)")]
        [SerializeField] private KeypadTiming keypad = new();

        // Timing of the expression builder: its intro plays as a whole, then the submit button follows.
        [Serializable]
        private sealed class BuilderTiming
        {
            [Tooltip("After QuestionBoxHolder is activated, wait this long before the builder's intro plays.")]
            [Min(0f)] public float builderDelay = 0.3f;
            [Tooltip("How long the builder's intro takes. Set it to the length of the ExpressionBuilderHolder animation. If the prefab has no Animator, the builder fades in over this time instead.")]
            [Min(0.01f)] public float builderAnimationTime = 0.35f;
            [Tooltip("After the builder's intro has finished, wait this long before the SubmitButton is activated.")]
            [Min(0f)] public float submitDelay = 0.15f;
        }

        [Header("Timing: expression builder (seconds)")]
        [SerializeField] private BuilderTiming builder = new();

        [Header("Timing: closing and after (seconds)")]
        [Tooltip("When Submit is pressed, how long QuestionAndAnswerHolder takes to fade back to 0.")]
        [SerializeField, Min(0.01f)] private float closeFadeDuration = 0.5f;
        [Tooltip("How long the blur takes to fade out when Submit is pressed. Set it equal to Close Fade Duration to make them match. (Blurring in uses Fade In Duration on the BlurBackdropController.)")]
        [SerializeField, Min(0.01f)] private float blurFadeOutDuration = 0.8f;
        [Tooltip("On: the player's answer line waits until BOTH the panel and the blur have finished fading, so nothing is still fading when the bubble appears. Off: it starts as soon as the panel has faded, while the blur may still be fading out behind it.")]
        [SerializeField] private bool waitForBothFades = true;
        [Tooltip("After the fade-out, extra pause before the player says their answer.")]
        [SerializeField, Min(0f)] private float afterAnswerDelay = 0f;
        [Tooltip("After the last reaction line, pause before the next question is asked.")]
        [SerializeField, Min(0f)] private float nextQuestionDelay = 0.3f;

        [Header("Health")]
        [Tooltip("Hearts lost per hit. A right answer hurts the enemy; a wrong answer hurts the player.")]
        [SerializeField, Min(1)] private int damagePerHit = 1;
        [Tooltip("After the player's answer line, how long the hit takes (hurt animation, heart lost) before the enemy replies.")]
        [SerializeField, Min(0f)] private float hitDuration = 0.6f;

        [Header("Player's answer line")]
        [Tooltip("What the player says before the answer. The answer follows it, for example: My answer is: -15")]
        [SerializeField] private string answerPrefix = "My answer is: ";

        [Header("Answer inputs")]
        [SerializeField] private MultipleChoicePanel multipleChoicePrefab;
        [Tooltip("The KeypadHolder prefab, used for questions answered with a number.")]
        [SerializeField] private NumberKeypadPanel keypadPrefab;
        [Tooltip("The ExpressionBuilderHolder prefab, used for questions answered by building an expression.")]
        [SerializeField] private ExpressionBuilderPanel builderPrefab;

        private readonly struct Submission
        {
            public readonly string text;
            public readonly bool correct;

            public Submission(string text, bool correct)
            {
                this.text = text;
                this.correct = correct;
            }
        }

        private int questionIndex;
        private List<QuestionResource> questions = new(); // built from the level's pools when the battle starts
        private GameObject answerUi;
        private QuestionResource activeQuestion;
        private Func<Submission?> readAnswer; // the open input's current answer, or null if none yet
        private Coroutine openRoutine;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            if (submitButton == null)
                Debug.LogError("BattleController needs the Submit Button assigned.", this);
            else
                submitButton.onClick.AddListener(OnSubmitPressed);

            if (questionAndAnswer == null)
                Debug.LogError("BattleController needs the Question And Answer CanvasGroup assigned.", this);
        }

        private void Start()
        {
            HidePanelContents();
            StartCoroutine(Begin());
        }

        private IEnumerator Begin()
        {
            if (characters != null)
                characters.Apply(level);

            yield return new WaitForSeconds(startDelay);
            questions = level.BuildQuestionList();
            questionIndex = 0;
            dialogue.PlayLines(level.startDialogue, AskQuestion);
        }

        private void AskQuestion()
        {
            if (questions.Count == 0 || questions[questionIndex] == null)
            {
                Debug.LogWarning("The level has no question to ask.", this);
                return;
            }

            QuestionResource question = questions[questionIndex];
            dialogue.SayAndHold(
                DialogueSpeaker.Enemy,
                question.question,
                () => openRoutine = StartCoroutine(OpenAnswerPanel(question)));
        }

        private IEnumerator OpenAnswerPanel(QuestionResource question)
        {
            // The question has finished typing; give the player a moment to read it.
            yield return new WaitForSeconds(readDelay);

            // The snapshot must be taken while the panel's contents are still hidden.
            yield return blur.Capture();

            // 1. Prepare everything while it is invisible, so nothing re-lays out later.
            SetGroup(0f, false);
            questionAndAnswer.gameObject.SetActive(true); // may be inactive in the scene; its children need it on
            answerArea.gameObject.SetActive(true);
            if (submitArea != null)
                submitArea.SetActive(true);
            submitButton.gameObject.SetActive(false); // appears after the input, with its own animation

            questionBox.Prepare(question.question); // fitted now; its own animation waits for Reveal
            SpawnAnswerInput(question);             // options spawned hidden, answer area sized for all of them
            questionBox.Conceal();

            // 2. The blur, then the holder, then the question box, then the options.
            blur.FadeIn();
            yield return new WaitForSeconds(holderFadeDelay);

            FadeGroup(1f, holderFadeDuration, true);
            yield return new WaitForSeconds(questionBoxDelay);

            if (answerFitter != null)
                answerFitter.SetFollowPaused(true); // the question box slides in; the answers stay put
            questionBox.Reveal();
            StartCoroutine(ResumeFollowAfter(questionBoxAnimationTime));

            yield return RevealInput();

            openRoutine = null;
        }

        private IEnumerator ResumeFollowAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (answerFitter != null)
                answerFitter.SetFollowPaused(false);
        }

        // The answer input's entrance, then the submit button. Each question type has its own timing.
        private IEnumerator RevealInput()
        {
            float submitDelay = 0f;

            if (answerUi != null && answerUi.TryGetComponent(out MultipleChoicePanel panel))
            {
                yield return RevealMultipleChoice(panel);
                submitDelay = multipleChoice.submitDelay;
            }
            else if (answerUi != null && answerUi.TryGetComponent(out NumberKeypadPanel pad))
            {
                yield return new WaitForSeconds(keypad.keypadDelay);
                pad.Reveal(keypad.keypadAnimationTime);
                yield return new WaitForSeconds(keypad.keypadAnimationTime);
                submitDelay = keypad.submitDelay;
            }
            else if (answerUi != null && answerUi.TryGetComponent(out ExpressionBuilderPanel expressionPanel))
            {
                yield return new WaitForSeconds(builder.builderDelay);
                expressionPanel.Reveal(builder.builderAnimationTime);
                yield return new WaitForSeconds(builder.builderAnimationTime);
                submitDelay = builder.submitDelay;
            }

            yield return new WaitForSeconds(submitDelay);
            submitButton.gameObject.SetActive(true); // plays the SubmitButton's animation
        }

        // Pops the options in one after another. They already exist, so the layout is final.
        private IEnumerator RevealMultipleChoice(MultipleChoicePanel panel)
        {
            yield return new WaitForSeconds(multipleChoice.optionsDelay);

            for (int i = 0; i < panel.OptionCount; i++)
            {
                panel.RevealOption(i);
                if (i < panel.OptionCount - 1)
                    yield return new WaitForSeconds(multipleChoice.optionInterval);
            }
        }

        private void SpawnAnswerInput(QuestionResource question)
        {
            switch (question.answer)
            {
                case MultipleChoiceAnswerResource choice:
                    MultipleChoicePanel panel = Instantiate(multipleChoicePrefab, answerArea, false);
                    answerUi = panel.gameObject;
                    activeQuestion = question;

                    // Tapping an option only selects it; Submit unlocks once something is selected.
                    panel.Show(choice, () => submitButton.interactable = panel.HasSelection, concealed: true);
                    submitButton.interactable = false;
                    readAnswer = () => panel.HasSelection
                        ? new Submission(
                            choice.GetOption(panel.SelectedIndex),
                            panel.SelectedIndex == choice.CorrectIndex)
                        : (Submission?)null;

                    if (answerFitter != null)
                        answerFitter.Fit(PreferredHeight(panel));
                    break;

                case NumberAnswerResource number:
                    if (keypadPrefab == null)
                    {
                        Debug.LogError("Assign the Keypad Prefab (the KeypadHolder prefab) on BattleController.", this);
                        break;
                    }

                    NumberKeypadPanel pad = Instantiate(keypadPrefab, answerArea, false);
                    answerUi = pad.gameObject;
                    activeQuestion = question;

                    // Any key press can change what is typed; Submit unlocks once something is.
                    pad.Show(() => submitButton.interactable = pad.HasValue);
                    pad.Conceal();
                    submitButton.interactable = false;
                    readAnswer = () => pad.TryGetValue(out double typed)
                        ? new Submission(pad.DisplayText, number.IsCorrect(typed))
                        : (Submission?)null;

                    if (answerFitter != null)
                        answerFitter.Fit(PreferredHeight(pad));
                    break;

                case ExpressionBuilderAnswerResource expression:
                    if (builderPrefab == null)
                    {
                        Debug.LogError("Assign the Builder Prefab (the ExpressionBuilderHolder prefab) on BattleController.", this);
                        break;
                    }

                    ExpressionBuilderPanel builderPanel = Instantiate(builderPrefab, answerArea, false);
                    answerUi = builderPanel.gameObject;
                    activeQuestion = question;

                    // Submit unlocks once every slot has a tile.
                    builderPanel.Show(expression, () => submitButton.interactable = builderPanel.AllFilled);
                    builderPanel.Conceal();
                    submitButton.interactable = false;
                    readAnswer = () => builderPanel.AllFilled
                        ? new Submission(builderPanel.AnswerText, expression.IsCorrect(builderPanel.PlacedTileIds))
                        : (Submission?)null;

                    if (answerFitter != null)
                        answerFitter.Fit(PreferredHeight(builderPanel));
                    break;

                default:
                    Debug.LogWarning(
                        $"No answer input exists yet for '{question.name}' ({question.answer}).", this);
                    break;
            }
        }

        // Height the spawned input's layout needs, for example the rows of a grid of options.
        private static float PreferredHeight(Component input)
        {
            var rect = (RectTransform)input.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            LayoutGroup group = input.GetComponent<LayoutGroup>();
            return group != null ? group.preferredHeight : rect.rect.height;
        }

        private void OnSubmitPressed()
        {
            Submission? submission = readAnswer?.Invoke();
            if (submission == null)
                return; // nothing selected yet

            // Submitting mid-cascade stops the options still popping in.
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            readAnswer = null;
            submitButton.interactable = false;
            if (answerUi != null && answerUi.TryGetComponent(out MultipleChoicePanel panel))
                panel.Lock();
            else if (answerUi != null && answerUi.TryGetComponent(out NumberKeypadPanel pad))
                pad.Lock();
            else if (answerUi != null && answerUi.TryGetComponent(out ExpressionBuilderPanel expressionPanel))
                expressionPanel.Lock();

            OnAnswered(activeQuestion, submission.Value.text, submission.Value.correct);
        }

        private void OnAnswered(QuestionResource question, string answerText, bool correct)
        {
            Debug.Log(correct ? "Correct answer." : "Wrong answer.");
            StartCoroutine(CloseAnswerPanel(BuildReaction(question, answerText, correct), correct));
        }

        // The player says their answer, then the enemy answers back: praise if it was right,
        // otherwise the taunt (optional) and the solution.
        private List<DialogueLine> BuildReaction(QuestionResource question, string answerText, bool correct)
        {
            var lines = new List<DialogueLine>
            {
                new DialogueLine { speaker = DialogueSpeaker.Player, text = answerPrefix + answerText }
            };

            if (correct)
            {
                AddEnemyLines(lines, question.encourageDialogue);
            }
            else
            {
                AddEnemyLines(lines, question.tauntDialogue);
                AddEnemyLines(lines, question.solutionDialogue);
            }

            return lines;
        }

        private static void AddEnemyLines(List<DialogueLine> into, List<string> texts)
        {
            if (texts == null)
                return;

            foreach (string text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    into.Add(new DialogueLine { speaker = DialogueSpeaker.Enemy, text = text });
            }
        }

        private IEnumerator CloseAnswerPanel(List<DialogueLine> reaction, bool correct)
        {
            // The enemy's bubble goes at once; the panel fades out and the blur fades with it.
            dialogue.ClearBubble();
            FadeGroup(0f, closeFadeDuration, false);
            blur.FadeOut(blurFadeOutDuration);

            float fadeTime = waitForBothFades
                ? Mathf.Max(closeFadeDuration, blurFadeOutDuration)
                : closeFadeDuration;
            yield return new WaitForSeconds(fadeTime + afterAnswerDelay);

            HidePanelContents();

            // The player says their answer first, then the hit lands, then the enemy replies.
            List<DialogueLine> answerLine = reaction.GetRange(0, 1);
            List<DialogueLine> reply = reaction.GetRange(1, reaction.Count - 1);
            dialogue.PlayLines(answerLine, () => StartCoroutine(HitThenReply(reply, correct)));
        }

        // A right answer hurts the enemy, a wrong one hurts the player.
        private IEnumerator HitThenReply(List<DialogueLine> reply, bool correct)
        {
            dialogue.ClearBubble();

            CharacterRole loser = correct ? CharacterRole.Enemy : CharacterRole.Player;
            if (characters != null)
            {
                characters.Damage(loser, damagePerHit);
                yield return new WaitForSeconds(hitDuration);
            }

            if (reply.Count > 0)
                dialogue.PlayLines(reply, AfterReply);
            else
                AfterReply();
        }

        private void AfterReply()
        {
            if (characters != null && characters.IsDefeated(CharacterRole.Enemy))
            {
                EndBattle(playerWon: true);
                return;
            }

            if (characters != null && characters.IsDefeated(CharacterRole.Player))
            {
                EndBattle(playerWon: false);
                return;
            }

            StartCoroutine(NextQuestion());
        }

        // Placeholder: the victory / defeat dialogue comes later. For now the battle just stops.
        private void EndBattle(bool playerWon)
        {
            Debug.Log(playerWon ? "Battle over: the enemy has no hearts left." : "Battle over: the player has no hearts left.");
        }

        // Temporary: right or wrong, move on and wrap around at the end.
        private IEnumerator NextQuestion()
        {
            yield return new WaitForSeconds(nextQuestionDelay);
            questionIndex = (questionIndex + 1) % questions.Count;
            AskQuestion();
        }

        // ---- QuestionAndAnswerHolder fade --------------------------------------------------

        private void SetGroup(float alpha, bool takesInput)
        {
            if (questionAndAnswer == null)
                return;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            questionAndAnswer.alpha = alpha;
            questionAndAnswer.interactable = takesInput;
            questionAndAnswer.blocksRaycasts = takesInput;
        }

        private void FadeGroup(float target, float duration, bool takesInput)
        {
            if (questionAndAnswer == null)
                return;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            // Fading in takes input from the start; fading out stops taking it at once.
            questionAndAnswer.interactable = takesInput;
            questionAndAnswer.blocksRaycasts = takesInput;
            fadeRoutine = StartCoroutine(FadeGroupRoutine(target, duration));
        }

        private IEnumerator FadeGroupRoutine(float target, float duration)
        {
            float from = questionAndAnswer.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                questionAndAnswer.alpha = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                yield return null;
            }

            questionAndAnswer.alpha = target;
            fadeRoutine = null;
        }

        private void HidePanelContents()
        {
            if (answerUi != null)
                Destroy(answerUi);

            answerUi = null;
            readAnswer = null;
            if (submitButton != null)
            {
                submitButton.interactable = false;
                submitButton.gameObject.SetActive(false);
            }

            if (submitArea != null)
                submitArea.SetActive(false);
            if (answerFitter != null)
            {
                answerFitter.Clear();
                answerFitter.SetFollowPaused(false);
            }

            questionBox.Hide();
            answerArea.gameObject.SetActive(false);
            SetGroup(0f, false);

            // Nothing in it is shown between questions, so the holder can rest switched off.
            if (questionAndAnswer != null)
                questionAndAnswer.gameObject.SetActive(false);
        }
    }
}
