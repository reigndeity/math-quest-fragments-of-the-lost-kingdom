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

        /// <summary>Shows one character on the side its role says.</summary>
        public void Apply(CharacterResource character)
        {
            bool isPlayer = character.role == CharacterRole.Player;
            CharacterSlot slot = isPlayer ? playerSlot : enemySlot;

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
            foreach (HeartView heart in slot.heartHolder.GetComponentsInChildren<HeartView>(false))
                heart.SetFilled(true);

            if (slot.heartHolder.TryGetComponent(out HeartRowLimiter limiter))
                limiter.Refresh();
        }
    }
}
