using System;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Runs dialogue in the battle. One chat box exists at a time: it is spawned under the
    /// holder of whoever is speaking and destroyed when the next line starts. Tapping the
    /// tap area finishes the line that is typing, or moves on to the next one.
    /// It only shows dialogue; BattleController decides what happens before and after.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleDialogueController : MonoBehaviour
    {
        [Header("Chat box")]
        [SerializeField] private DialogueChatBox chatBoxPrefab;
        [Tooltip("Where the player's bubble spawns (PlayerDialogueHolder).")]
        [SerializeField] private RectTransform playerDialogueHolder;
        [Tooltip("Where the enemy's bubble spawns (EnemyDialogueHolder).")]
        [SerializeField] private RectTransform enemyDialogueHolder;

        [Header("Chat box (tune live in Play mode)")]
        [FormerlySerializedAs("chatBoxFit")]
        [SerializeField] private ChatBoxSettings chatBox = new();

        [Header("Input")]
        [Tooltip("Button covering the screen. Tapping it advances the dialogue. It is only made non-interactable when idle, never deactivated, so it can safely sit on a parent such as BattleArea.")]
        [SerializeField] private Button tapArea;

        private enum Mode { Idle, Lines, Hold }

        private Mode mode;
        private DialogueChatBox current;
        private IReadOnlyList<DialogueLine> lines;
        private int index;
        private Action onFinished;
        private (float, float, float, float, float, float, float, ChatBoxGrow, bool, float, float, ChatBoxSide) fitKey;

        private void Awake()
        {
            if (tapArea != null)
            {
                tapArea.onClick.AddListener(OnTap);
                tapArea.interactable = false;
            }
        }

        // Refits the bubble on screen as soon as a fit setting changes in the Inspector.
        private void Update()
        {
            if (current == null || chatBox.Key == fitKey)
                return;

            fitKey = chatBox.Key;
            current.Refit();
        }

        /// <summary>
        /// Plays the lines in order, one bubble at a time, then calls <paramref name="finished"/>.
        /// The last bubble is removed when the player taps past it.
        /// </summary>
        public void PlayLines(IReadOnlyList<DialogueLine> dialogue, Action finished)
        {
            if (dialogue == null || dialogue.Count == 0)
            {
                finished?.Invoke();
                return;
            }

            lines = dialogue;
            index = 0;
            onFinished = finished;
            mode = Mode.Lines;
            SetTappable(true);
            ShowLine();
        }

        /// <summary>
        /// One speaker says one line and the bubble stays up. <paramref name="typed"/> fires
        /// as soon as the line has finished typing (or a tap finished it). Remove the bubble
        /// later with <see cref="ClearBubble"/>.
        /// </summary>
        public void SayAndHold(DialogueSpeaker speaker, string text, Action typed)
        {
            mode = Mode.Hold;
            onFinished = typed;
            SetTappable(true);

            DialogueChatBox box = Spawn(speaker, text);
            if (box == null)
            {
                Release();
                return;
            }

            box.Typed += OnHeldTyped;
            if (!box.IsTyping)
                OnHeldTyped();
        }

        public void ClearBubble()
        {
            DestroyChatBox();
        }

        private void ShowLine()
        {
            DialogueLine line = lines[index];
            Spawn(line.speaker, line.text);
        }

        private DialogueChatBox Spawn(DialogueSpeaker speaker, string text)
        {
            DestroyChatBox();

            bool isPlayer = speaker == DialogueSpeaker.Player;
            RectTransform holder = isPlayer ? playerDialogueHolder : enemyDialogueHolder;

            if (holder == null)
            {
                Debug.LogWarning($"No dialogue holder is assigned for the {speaker}.", this);
                return null;
            }

            current = Instantiate(chatBoxPrefab, holder, false);
            // The prefab is laid out for the enemy; the player's faces the other way.
            current.SetMirrored(isPlayer);
            fitKey = chatBox.Key;
            current.Show(text, chatBox);
            return current;
        }

        private void OnHeldTyped()
        {
            if (mode != Mode.Hold)
                return;

            Action callback = onFinished;
            Release();
            callback?.Invoke();
        }

        private void OnTap()
        {
            switch (mode)
            {
                case Mode.Hold:
                    if (current != null && current.IsTyping)
                        current.Skip();
                    break;

                case Mode.Lines:
                    if (current != null && current.IsTyping)
                    {
                        current.Skip();
                        break;
                    }

                    index++;
                    if (index < lines.Count)
                    {
                        ShowLine();
                        break;
                    }

                    DestroyChatBox();
                    Action callback = onFinished;
                    Release();
                    callback?.Invoke();
                    break;
            }
        }

        private void Release()
        {
            mode = Mode.Idle;
            onFinished = null;
            SetTappable(false);
        }

        private void SetTappable(bool tappable)
        {
            if (tapArea != null)
                tapArea.interactable = tappable;
        }

        private void DestroyChatBox()
        {
            if (current != null)
                Destroy(current.gameObject);

            current = null;
        }
    }
}
