using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>The three anchor points on a character's AnchorVFX (Root, Center, Top children).</summary>
    public enum VfxAnchorPoint
    {
        Root = 0,
        Center = 1,
        Top = 2
    }

    /// <summary>Which character a skill's effect appears on.</summary>
    public enum SkillPlacement
    {
        /// <summary>On the character being hit.</summary>
        OnTarget = 0,
        /// <summary>On the character using the skill.</summary>
        OnCaster = 1
    }

    /// <summary>
    /// One attack or skill: the effect that is spawned, where it appears, and when the hit
    /// lands. A character picks one of these per question difficulty.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillResource_Player_Easy",
        menuName = "Math Quest/Resources/Skill Resource")]
    public sealed class SkillResource : ScriptableObject
    {
        [Tooltip("The effect that is spawned (for example a Pixel Arsenal prefab). Empty means no effect: the hit still lands.")]
        public GameObject vfxPrefab;

        [Tooltip("On Target: the effect appears on the character being hit. On Caster: on the one using the skill.")]
        public SkillPlacement placement = SkillPlacement.OnTarget;

        [Tooltip("Which of the character's anchor points (Root, Center or Top) the effect appears at.")]
        public VfxAnchorPoint anchor = VfxAnchorPoint.Center;

        [Tooltip("Seconds from the effect appearing until the hit lands (heart lost, hurt animation).")]
        [Min(0f)] public float impactDelay = 0.3f;

        [Tooltip("Seconds after which the spawned effect is removed.")]
        [Min(0.1f)] public float lifetime = 2f;

        [Tooltip("Multiplies the effect's size.")]
        [Min(0.01f)] public float scale = 1f;

        [Tooltip("Drawing order of the effect. It must be higher than the Canvas's Sorting Order so it appears in front of the characters.")]
        public int sortingOrder = 100;
    }
}
