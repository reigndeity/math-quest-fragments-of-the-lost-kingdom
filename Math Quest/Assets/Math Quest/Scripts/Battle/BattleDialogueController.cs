using System;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>The typing sound: a short beep as letters appear, a different voice for each side.</summary>
    [Serializable]
    public sealed class DialogueSoundSettings
    {
        [Tooltip("The beeps. One is picked at random each time, so the voice does not sound like a machine gun.")]
        public AudioClip[] beeps;

        [Range(0f, 1f)] public float volume = 0.5f;

        [Tooltip("A beep plays on every Nth letter. 1 is every letter, 2 is every other letter. Higher is calmer.")]
        [Min(1)] public int everyNthLetter = 2;

        [Tooltip("Each beep's pitch is random between these two, so repeated beeps vary a little.")]
        [Min(0.1f)] public float pitchMin = 0.95f;
        [Min(0.1f)] public float pitchMax = 1.05f;

        [Tooltip("Multiplies the pitch for the player's bubbles. Above 1 is higher.")]
        [Min(0.1f)] public float playerPitch = 1.2f;
        [Tooltip("Multiplies the pitch for the enemy's bubbles. Below 1 is deeper.")]
        [Min(0.1f)] public float enemyPitch = 0.85f;

        [Tooltip("On: spaces beep too. Off: only letters do.")]
        public bool beepOnSpaces;
    }

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

        [Header("Typing sound (tune live in Play mode)")]
        [SerializeField] private DialogueSoundSettings sound = new();

        [Header("Input")]
        [Tooltip("Button covering the screen. Tapping it advances the dialogue. It is never deactivated, only made non-interactable while idle. Keep it above AnswerPanel in the hierarchy so the answer options are drawn, and clickable, on top of it.")]
        [SerializeField] private Button tapArea;

        private enum Mode { Idle, Lines, Hold }

        private Mode mode;
        private DialogueChatBox current;
        private AudioSource beepSource;
        private int letterCount;
        private float voicePitch = 1f;
        private IReadOnlyList<DialogueLine> lines;
        private int index;
        private Action onFinished;
        private int fitHash;

        private void Awake()
        {
            if (tapArea != null)
            {
                tapArea.onClick.AddListener(OnTap);
                SetTappable(false);
            }
        }

        // Refits the bubble on screen as soon as a fit setting changes in the Inspector.
        private void Update()
        {
            if (current == null || chatBox.Hash == fitHash)
                return;

            fitHash = chatBox.Hash;
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
            fitHash = chatBox.Hash;

            // The voice: set up before the first letter appears.
            letterCount = 0;
            voicePitch = isPlayer ? sound.playerPitch : sound.enemyPitch;
            current.LetterTyped += OnLetterTyped;

            current.Show(text, chatBox);
            return current;
        }

        private void OnLetterTyped(char letter)
        {
            if (sound.beeps == null || sound.beeps.Length == 0)
                return;

            if (!sound.beepOnSpaces && char.IsWhiteSpace(letter))
                return;

            letterCount++;
            if (letterCount % Mathf.Max(1, sound.everyNthLetter) != 0)
                return;

            AudioClip clip = sound.beeps[UnityEngine.Random.Range(0, sound.beeps.Length)];
            if (clip == null)
                return;

            if (beepSource == null)
            {
                beepSource = gameObject.AddComponent<AudioSource>();
                beepSource.playOnAwake = false;
                beepSource.spatialBlend = 0f; // plain 2D sound
            }

            beepSource.pitch = UnityEngine.Random.Range(sound.pitchMin, sound.pitchMax) * voicePitch;
            beepSource.PlayOneShot(clip, sound.volume);
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
