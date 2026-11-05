using System;
using System.Collections.Generic;
using Febucci.UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Plays a list of dialogue lines with the Text Animator typewriter. Tapping while a
    /// line is typing finishes it; tapping again moves to the next line.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueBox : MonoBehaviour
    {
        [SerializeField] private TypewriterCore typewriter;
        [Tooltip("Button covering the dialogue area. Tapping it advances the dialogue.")]
        [SerializeField] private Button advanceButton;

        private IReadOnlyList<string> lines;
        private int index;
        private Action onFinished;

        public bool IsPlaying { get; private set; }

        private void Awake()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(Advance);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Shows the lines in order, then calls <paramref name="finished"/>. Null or empty
        /// lines finish immediately, which is how optional dialogue is skipped.
        /// </summary>
        public void Play(IReadOnlyList<string> dialogueLines, Action finished = null)
        {
            if (dialogueLines == null || dialogueLines.Count == 0)
            {
                finished?.Invoke();
                return;
            }

            lines = dialogueLines;
            index = 0;
            onFinished = finished;
            IsPlaying = true;
            gameObject.SetActive(true);
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            typewriter.ShowText(lines[index]);
        }

        private void Advance()
        {
            if (!IsPlaying)
                return;

            if (typewriter.isShowingText)
            {
                typewriter.SkipTypewriter();
                return;
            }

            index++;
            if (index < lines.Count)
            {
                ShowCurrentLine();
                return;
            }

            IsPlaying = false;
            gameObject.SetActive(false);
            Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }
    }
}
