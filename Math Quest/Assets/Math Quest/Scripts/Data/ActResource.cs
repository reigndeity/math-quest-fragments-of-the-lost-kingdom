using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>One level of an act and where it sits on the act's level map.</summary>
    [Serializable]
    public sealed class ActLevelSlot
    {
        [Tooltip("The level played from this spot. Empty is a placeholder you can fill later.")]
        public LevelResource level;

        [Tooltip("Where the level sits on the map, in UI units: x from the middle of a 1080-wide map, y up from the bottom.")]
        public Vector2 mapPosition;
    }

    /// <summary>
    /// One Act: its levels in play order (each with its place on the level map), and the background every
    /// one of its battles uses. Lay the map out in Math Quest > Act Level Designer.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ActResource_Act1",
        menuName = "Math Quest/Resources/Act Resource")]
    public sealed class ActResource : ScriptableObject
    {
        /// <summary>Map positions are authored for a map this wide; a wider or narrower screen scales x to match.</summary>
        public const float MapReferenceWidth = 1080f;

        public ActId act = ActId.Act1;

        public string displayName;

        [Tooltip("The levels of this act in play order, level 1 first, each with its place on the level map. Edit them in Math Quest > Act Level Designer.")]
        public List<ActLevelSlot> levels = new();

        [Tooltip("Space above the highest level on the map, so the scroll stops a little past it.")]
        [Min(0f)] public float mapTopPadding = 400f;

        [Tooltip("The battle background for every level of this act.")]
        public Sprite background;

        [Tooltip("Optional. A prefab spawned into Environment > AmbienceHolder for every battle of this act, "
                 + "for example falling leaves, fireflies or dust (a Pixel Arsenal Environment effect or your own).")]
        public GameObject ambiencePrefab;

        [Tooltip("Drawing order of the ambience's particles. Higher than the background (0) and lower than the "
                 + "characters (enemy 99, player 100) puts it between them. Only used for particle effects, not UI.")]
        public int ambienceSortingOrder = 10;

        /// <summary>How tall the level map is: the highest level plus Map Top Padding. 0 with no levels.</summary>
        public float MapHeight
        {
            get
            {
                if (levels.Count == 0)
                    return 0f;

                float top = 0f;
                foreach (ActLevelSlot slot in levels)
                {
                    if (slot != null)
                        top = Mathf.Max(top, slot.mapPosition.y);
                }

                return top + mapTopPadding;
            }
        }

        public bool Contains(LevelResource level) => levels.Exists(slot => slot != null && slot.level == level);
    }
}
