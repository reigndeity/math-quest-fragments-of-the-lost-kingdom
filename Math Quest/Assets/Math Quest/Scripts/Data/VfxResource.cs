using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>When in a skill a step happens.</summary>
    public enum VfxTrigger
    {
        /// <summary>The skill begins (for an attack: the PlaySkill animation event).</summary>
        Start = 0,
        /// <summary>The hit lands: the moment a heart is lost.</summary>
        Impact = 1,
        /// <summary>The skill is over, just before the reply.</summary>
        End = 2
    }

    /// <summary>What a step does.</summary>
    public enum VfxStepKind
    {
        /// <summary>Spawns an effect prefab at a character.</summary>
        Spawn = 0,
        /// <summary>Shakes the battle scene.</summary>
        CameraShake = 1
    }

    /// <summary>Which character a step works with.</summary>
    public enum VfxPosition
    {
        Caster = 0,
        Target = 1
    }

    public enum VfxRotationMode
    {
        /// <summary>The rotation the prefab has.</summary>
        Prefab = 0,
        Custom = 1
    }

    public enum VfxScaleMode
    {
        /// <summary>The prefab's own size.</summary>
        Prefab = 0,
        /// <summary>The prefab's own size times a number.</summary>
        Multiply = 1,
        Custom = 2
    }

    /// <summary>Marks a string field as the id of a VfxResource. The inspector shows a dropdown of all of them.</summary>
    public sealed class VfxIdAttribute : PropertyAttribute
    {
    }

    /// <summary>One thing a VFX does: spawn an effect at a character, or shake the screen.</summary>
    [Serializable]
    public sealed class VfxStep
    {
        [Tooltip("Spawn an effect, or shake the battle scene.")]
        public VfxStepKind kind = VfxStepKind.Spawn;

        [Tooltip("When this step happens during the skill.")]
        public VfxTrigger trigger = VfxTrigger.Start;

        [Tooltip("Seconds to wait after the trigger before spawning.")]
        [Min(0f)] public float delay;

        [Tooltip("The effect to spawn, for example a Pixel Arsenal prefab or one of yours.")]
        public GameObject prefab;

        [Tooltip("Which character the effect appears at.")]
        public VfxPosition position = VfxPosition.Caster;

        [Tooltip("Which point on that character: Root, Center or Top.")]
        public VfxAnchorPoint anchor = VfxAnchorPoint.Center;

        [Tooltip("Moves the effect away from its anchor, in world units (the Canvas is small in the world, so use small numbers).")]
        public Vector3 offset;

        public VfxRotationMode rotationMode = VfxRotationMode.Prefab;
        public Vector3 rotation;

        public VfxScaleMode scaleMode = VfxScaleMode.Prefab;
        [Min(0.01f)] public float scale = 1f;
        public Vector3 customScale = Vector3.one;

        [Tooltip("On: the effect follows the character it appears at while it plays.")]
        public bool parentToAnchor;

        [Tooltip("On: the effect flies from where it spawns to the point below, like a projectile.")]
        public bool travel;

        public VfxPosition travelTo = VfxPosition.Target;
        public VfxAnchorPoint travelAnchor = VfxAnchorPoint.Center;

        [Tooltip("Seconds the flight takes. Set the VFX's Impact Delay to match so the hit lands on arrival.")]
        [Min(0.01f)] public float travelDuration = 0.4f;

        [Tooltip("Seconds after which the effect is removed.")]
        [Min(0.1f)] public float lifetime = 2f;

        [Tooltip("Drawing order. It must be higher than the Canvas's Sorting Order so the effect shows in front of the characters.")]
        public int sortingOrder = 100;

        // ---- camera shake (only used when Kind is Camera Shake) ----

        [Tooltip("How long the shake lasts, in seconds.")]
        [Min(0.05f)] public float shakeDuration = 0.3f;

        [Tooltip("How far the scene moves at the strongest point, in canvas units (pixels at the reference size). Around 10 is a rumble, 40 is a hard hit.")]
        [Min(0f)] public float shakeStrength = 20f;

        [Tooltip("How many times a second the shake jumps to a new spot.")]
        [Min(1f)] public float shakeFrequency = 30f;

        [Tooltip("Strength over the shake's length: left is the start, right is the end, height is how strong (1 = full Strength). A falling line is a hit; a rising line is a build-up rumble.")]
        public AnimationCurve shakeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    }

    /// <summary>
    /// A skill's visual effect: a list of steps that happen at the start of the skill, at the
    /// moment of impact, or at its end. Skills refer to it by Id. Keep these assets in a
    /// Resources/VFX folder: that is where the game looks for them.
    /// </summary>
    [CreateAssetMenu(
        fileName = "VFX_New",
        menuName = "Math Quest/Resources/VFX")]
    public sealed class VfxResource : ScriptableObject
    {
        [Tooltip("The name skills use to refer to this VFX. Leave empty to use the asset's name. Must be unique.")]
        public string id;

        [Tooltip("Seconds from the skill starting until the hit lands. This is the moment a heart is lost and the Impact steps play. For a charge-up, make it as long as the charge. If a step makes an effect travel, make it match the travel time.")]
        [Min(0f)] public float impactDelay = 0.4f;

        [Tooltip("Seconds after the hit before the End steps play and the reply starts. Leave room for the impact effect to be seen.")]
        [Min(0f)] public float replyDelay = 0.6f;

        public List<VfxStep> steps = new();

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
    }
}
