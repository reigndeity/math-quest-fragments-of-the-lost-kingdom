using System;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>The scene pieces that show one fighter.</summary>
    [Serializable]
    public sealed class CharacterSlot
    {
        [Tooltip("PlayerModel or EnemyModel. The character prefab is spawned inside it.")]
        public RectTransform modelHolder;

        // The character spawned last, so it can be replaced.
        [NonSerialized] public GameObject spawned;

        // Hearts left. Set when the character is shown.
        [NonSerialized] public int health;

        // Who is shown here right now.
        [NonSerialized] public CharacterResource character;

        [Tooltip("The NameText in the CharacterStatsHolder.")]
        public TMP_Text nameText;

        [Tooltip("The HeartHolder. Hearts are added or removed here to match the character's max hearts.")]
        public RectTransform heartHolder;
    }

    /// <summary>
    /// Connects the Battle scene to CharacterResources. The scene stays the same for every
    /// level; the player comes from this component, and the enemy from the level.
    /// </summary>
    public sealed class CharacterAssetConfig : MonoBehaviour
    {
        [Header("Characters")]
        [Tooltip("The player's character asset.")]
        [SerializeField] private CharacterResource player;

        [Header("Player side")]
        [SerializeField] private CharacterSlot playerSlot = new();

        [Header("Enemy side")]
        [SerializeField] private CharacterSlot enemySlot = new();

        [Header("Character prefab")]
        [Tooltip("On: the spawned prefab's root is stretched to fill the holder. Off: it keeps the size it has in its prefab.")]
        [SerializeField] private bool fitToHolder = true;

        [Header("Hearts")]
        [Tooltip("The HealthHearts prefab, spawned into each HeartHolder.")]
        [SerializeField] private GameObject heartPrefab;

        private string playerNameOverride;

        /// <summary>Sets what the player typed. Empty goes back to the asset's Display Name.</summary>
        public void SetPlayerName(string typedName)
        {
            playerNameOverride = typedName;
            if (player != null)
                Apply(player);
        }

        /// <summary>Shows the player and the level's enemy.</summary>
        public void Apply(LevelResource level)
        {
            if (player != null)
                Apply(player);
            else
                Debug.LogWarning("CharacterAssetConfig has no Player character assigned.", this);

            if (level != null && level.enemy != null)
                Apply(level.enemy);
            else
                Debug.LogWarning("The level has no Enemy character assigned.", this);
        }

        /// <summary>Hearts left for this side.</summary>
        public int Health(CharacterRole role) => SlotFor(role).health;

        public bool IsDefeated(CharacterRole role) => SlotFor(role).health <= 0;

        /// <summary>
        /// Takes hearts from one side: the last full heart loses its fill, and the hurt
        /// animation plays if the character prefab has an Animator with a "hurt" trigger.
        /// </summary>
        public void Damage(CharacterRole role, int amount = 1)
        {
            CharacterSlot slot = SlotFor(role);
            slot.health = Mathf.Max(0, slot.health - amount);
            ShowFilledHearts(slot);
            PlayHurt(slot);
        }

        /// <summary>The skill the character on this side uses for a question difficulty, or null.</summary>
        public SkillResource GetSkill(CharacterRole role, QuestionDifficulty difficulty)
        {
            CharacterResource shown = SlotFor(role).character;
            return shown != null ? shown.GetSkill(difficulty) : null;
        }

        /// <summary>The anchor points (Root, Center, Top) of the character shown on this side, or null.</summary>
        public VfxAnchors GetAnchors(CharacterRole role)
        {
            GameObject spawned = SlotFor(role).spawned;
            return spawned != null ? spawned.GetComponentInChildren<VfxAnchors>() : null;
        }

        private CharacterSlot SlotFor(CharacterRole role) => role == CharacterRole.Player ? playerSlot : enemySlot;

        private static void ShowFilledHearts(CharacterSlot slot)
        {
            if (slot.heartHolder == null)
                return;

            HeartView[] hearts = slot.heartHolder.GetComponentsInChildren<HeartView>(false);
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].SetFilled(i < slot.health);
        }

        private static void PlayHurt(CharacterSlot slot)
        {
            TrySetTrigger(slot, "hurt");
        }

        /// <summary>
        /// Sets a trigger on the first Animator that has it: the one on the holder (PlayerModel or
        /// EnemyModel), or one inside the spawned character prefab. False if none has it.
        /// </summary>
        private static bool TrySetTrigger(CharacterSlot slot, string trigger)
        {
            var animators = new System.Collections.Generic.List<Animator>();
            if (slot.modelHolder != null && slot.modelHolder.TryGetComponent(out Animator onHolder))
                animators.Add(onHolder);
            if (slot.spawned != null)
                animators.AddRange(slot.spawned.GetComponentsInChildren<Animator>());

            foreach (Animator animator in animators)
            {
                if (animator.runtimeAnimatorController == null)
                    continue;

                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger)
                    {
                        animator.SetTrigger(trigger);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>The Animator trigger that starts the attack for a difficulty: easyAtk, moderateAtk or difficultAtk.</summary>
        public static string AttackTrigger(QuestionDifficulty difficulty)
        {
            return difficulty switch
            {
                QuestionDifficulty.Moderate => "moderateAtk",
                QuestionDifficulty.Difficult => "difficultAtk",
                _ => "easyAtk"
            };
        }

        /// <summary>
        /// Starts this side's attack animation for the difficulty. False if there is no such
        /// animation (no Animator, or no matching trigger), so the caller can skip waiting for it.
        /// </summary>
        public bool TryPlayAttack(CharacterRole role, QuestionDifficulty difficulty)
        {
            return TrySetTrigger(SlotFor(role), AttackTrigger(difficulty));
        }

        /// <summary>
        /// The receiver of this side's animation events (on PlayerModel or EnemyModel, next to the
        /// Animator). Added automatically if the holder has an Animator but no receiver yet.
        /// </summary>
        public CharacterAnimationEvents GetAnimationEvents(CharacterRole role)
        {
            RectTransform holder = SlotFor(role).modelHolder;
            if (holder == null)
                return null;

            if (!holder.TryGetComponent(out CharacterAnimationEvents events) && holder.TryGetComponent(out Animator _))
                events = holder.gameObject.AddComponent<CharacterAnimationEvents>();

            return events;
        }

        /// <summary>Shows one character on the side its role says.</summary>
        public void Apply(CharacterResource character)
        {
            bool isPlayer = character.role == CharacterRole.Player;
            CharacterSlot slot = isPlayer ? playerSlot : enemySlot;

            slot.character = character;
            SpawnModel(slot, character.characterPrefab);

            if (slot.nameText != null)
            {
                slot.nameText.text = isPlayer && !string.IsNullOrEmpty(playerNameOverride)
                    ? playerNameOverride
                    : character.displayName;
            }

            ShowHearts(slot, character.maxHearts);

            if (slot.nameText != null && slot.nameText.transform.parent is RectTransform statsBox)
                LayoutRebuilder.ForceRebuildLayoutImmediate(statsBox);
        }

        private void SpawnModel(CharacterSlot slot, GameObject prefab)
        {
            if (slot.modelHolder == null || prefab == null)
                return;

            if (slot.spawned != null)
                Destroy(slot.spawned);

            slot.spawned = Instantiate(prefab, slot.modelHolder, false);
            slot.spawned.name = prefab.name;

            if (fitToHolder && slot.spawned.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }

        private void ShowHearts(CharacterSlot slot, int count)
        {
            if (slot.heartHolder == null)
                return;

            int shown = 0;
            foreach (Transform child in slot.heartHolder)
            {
                if (child.gameObject.activeSelf)
                    shown++;
            }

            // Remove extras from the end.
            for (int i = slot.heartHolder.childCount - 1; i >= 0 && shown > count; i--)
            {
                Transform child = slot.heartHolder.GetChild(i);
                if (!child.gameObject.activeSelf)
                    continue;

                child.gameObject.SetActive(false); // the layout stops counting it right away
                Destroy(child.gameObject);
                shown--;
            }

            if (heartPrefab == null && shown < count)
                Debug.LogError("CharacterAssetConfig needs the Heart Prefab (HealthHearts) assigned.", this);

            while (shown < count && heartPrefab != null)
            {
                Instantiate(heartPrefab, slot.heartHolder, false);
                shown++;
            }

            // Every heart starts full.
            slot.health = count;
            ShowFilledHearts(slot);

            if (slot.heartHolder.TryGetComponent(out HeartRowLimiter limiter))
                limiter.Refresh();
        }
    }
}
