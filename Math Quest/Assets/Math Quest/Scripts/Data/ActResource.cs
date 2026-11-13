using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>
    /// One Act: its levels, in order, and the background every one of its battles uses. The act and
    /// level selection screens will read their lists from here.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActResource_Act1",
        menuName = "Math Quest/Resources/Act Resource")]
    public sealed class ActResource : ScriptableObject
    {
        public ActId act = ActId.Act1;

        public string displayName;

        [Tooltip("The levels of this act, in the order they are played.")]
        public List<LevelResource> levels = new();

        [Tooltip("The battle background for every level of this act.")]
        public Sprite background;

        [Tooltip("Optional. A prefab spawned into Environment > AmbienceHolder for every battle of this act, "
                 + "for example falling leaves, fireflies or dust (a Pixel Arsenal Environment effect or your own).")]
        public GameObject ambiencePrefab;

        [Tooltip("Drawing order of the ambience's particles. Higher than the background (0) and lower than the "
                 + "characters (enemy 99, player 100) puts it between them. Only used for particle effects, not UI.")]
        public int ambienceSortingOrder = 10;
    }
}
