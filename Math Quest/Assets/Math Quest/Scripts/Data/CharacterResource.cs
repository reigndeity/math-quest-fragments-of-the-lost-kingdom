using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One fighter: who they are, how many hearts they start with, and how they look in battle.
    /// The player is a special case: its name will later come from what the player types, and
    /// Display Name is only the default.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CharacterResource_Enemy",
        menuName = "Math Quest/Resources/Character Resource")]
    public sealed class CharacterResource : ScriptableObject
    {
        [Tooltip("Player or Enemy. Decides which side of the battle scene this character is shown on.")]
        public CharacterRole role = CharacterRole.Enemy;

        [Tooltip("Shown in the character's stats box. For the player this is only the default name.")]
        public string displayName = "NAME";

        [Tooltip("How many hearts this character starts the battle with.")]
        [Min(1)] public int maxHearts = 3;

        [Tooltip("The character prefab (for example Player_Boy). It is spawned into the PlayerModel or EnemyModel holder when the battle starts. If empty, whatever is already in the holder is kept.")]
        public GameObject characterPrefab;
    }
}
