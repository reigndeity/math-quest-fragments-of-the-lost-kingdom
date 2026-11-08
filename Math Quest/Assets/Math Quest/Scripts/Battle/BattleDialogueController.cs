using MathQuest.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Plays a level's start dialogue. One chat box exists at a time: it is spawned under
    /// the holder of whoever is speaking and destroyed when the next line starts. Tapping
    /// the tap area finishes the line that is typing, or moves on to the next one.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleDialogueController : MonoBehaviour
    {
        [SerializeField] private LevelResource level;

        [Header("Chat box")]
        [SerializeField] private DialogueChatBox chatBoxPrefab;
        [Tooltip("Where the player's bubble spawns (PlayerDialogueHolder).")]
        [SerializeField] private RectTransform playerDialogueHolder;
        [Tooltip("Where the enemy's bubble spawns (EnemyDialogueHolder).")]
        [SerializeField] private RectTransform enemyDialogueHolder;

        [Header("Input")]
        [Tooltip("Button covering the screen. Tapping it advances the dialogue.")]
        [SerializeField] private Button tapArea;

        [Header("Events")]
        [SerializeField] private UnityEvent onStartDialogueFinished;

        private DialogueChatBox current;
        private int index;
        private bool playing;

        private void Awake()
        {
            if (tapArea != null)
            {
                tapArea.onClick.AddListener(OnTap);
                tapArea.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            PlayStartDialogue();
        }

        public void PlayStartDialogue()
        {
            if (level == null || level.startDialogue.Count == 0)
            {
                Finish();
                return;
            }

            index = 0;
            playing = true;
            if (tapArea != null) tapArea.gameObject.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            DestroyChatBox();

            DialogueLine line = level.startDialogue[index];
            bool isPlayer = line.speaker == DialogueSpeaker.Player;
            RectTransform holder = isPlayer ? playerDialogueHolder : enemyDialogueHolder;

            if (holder == null)
            {
                Debug.LogWarning($"No dialogue holder is assigned for the {line.speaker}.", this);
                return;
            }

            current = Instantiate(chatBoxPrefab, holder, false);
            // The prefab is laid out for the enemy; the player's faces the other way.
            current.SetMirrored(isPlayer);
            current.Show(line.text);
        }

        private void OnTap()
        {
            if (!playing)
                return;

            if (current != null && current.IsTyping)
            {
                current.Skip();
                return;
            }

            index++;
            if (index < level.startDialogue.Count)
            {
                ShowLine();
                return;
            }

            playing = false;
            DestroyChatBox();
            Finish();
        }

        private void Finish()
        {
            if (tapArea != null) tapArea.gameObject.SetActive(false);
            onStartDialogueFinished?.Invoke();
        }

        private void DestroyChatBox()
        {
            if (current != null)
                Destroy(current.gameObject);

            current = null;
        }
    }
}
