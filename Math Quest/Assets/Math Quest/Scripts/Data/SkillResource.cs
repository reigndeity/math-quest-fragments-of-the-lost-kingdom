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
        [Tooltip("The VFX this skill plays: pick one from the list. A VFX can spawn effects on the caster, on the target, let one fly across, and play something on impact. Empty: the simple effect below is used instead.")]
        [VfxId] public string vfxId;

        [Tooltip("The Animator trigger that starts this skill's attack animation, for example Attack_JumpAndLand. "
                 + "Empty: the trigger for the question's difficulty is used (easyAtk, moderateAtk or difficultAtk). "
                 + "Naming it here lets a character own several different attacks.")]
        public string attackTrigger;

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

        [Tooltip("On: the effect is spawned with the Rotation below instead of the rotation it has in its prefab.")]
        public bool overrideRotation;

        [Tooltip("Rotation in degrees. Only used when Override Rotation is on.")]
        public Vector3 rotation;

        [Tooltip("Off: the prefab's own size times Scale Multiplier. On: the Custom Scale below is used instead.")]
        public bool overrideScale;

        [Tooltip("Multiplies the prefab's own size. Only used when Override Scale is off.")]
        [Min(0.01f)] public float scale = 1f;

        [Tooltip("The size the effect is spawned with. Only used when Override Scale is on.")]
        public Vector3 customScale = Vector3.one;

        [Tooltip("Moves the effect away from its anchor, in world units (the Canvas is small in the world, so use small numbers).")]
        public Vector3 positionOffset;

        [Tooltip("Drawing order of the effect. It must be higher than the Canvas's Sorting Order so it appears in front of the characters.")]
        public int sortingOrder = 100;
    }
}
