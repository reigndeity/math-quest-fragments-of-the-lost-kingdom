using System.Collections;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// One skill being played. The battle starts it, then asks it for the Impact and End steps
    /// at the right moments.
    /// </summary>
    public sealed class VfxRun
    {
        private readonly VfxManager manager;
        internal readonly List<VfxStep> Steps;
        internal readonly CharacterRole Caster;
        internal readonly CharacterRole Target;

        /// <summary>Seconds from the start until the hit should land (a heart is lost).</summary>
        public float ImpactDelay { get; }

        /// <summary>Seconds after the hit before the reply. Negative: not set, use the battle's own Hit Duration.</summary>
        public float ReplyDelay { get; }

        internal VfxRun(VfxManager manager, List<VfxStep> steps, CharacterRole caster, CharacterRole target,
            float impactDelay, float replyDelay = -1f)
        {
            ReplyDelay = replyDelay;
            this.manager = manager;
            Steps = steps;
            Caster = caster;
            Target = target;
            ImpactDelay = impactDelay;
        }

        /// <summary>Plays the steps that happen when the hit lands.</summary>
        public void Impact() => manager.Fire(this, VfxTrigger.Impact);

        /// <summary>Plays the steps that happen when the skill is over.</summary>
        public void End() => manager.Fire(this, VfxTrigger.End);
    }

    /// <summary>
    /// Plays skills. A skill points at a VFX by id (a VfxResource in a Resources/VFX folder), and
    /// the VFX lists what to spawn where and when. The battle only says "begin this skill from
    /// caster to target", then "impact" and "end".
    /// </summary>
    public sealed class VfxManager : MonoBehaviour
    {
        private const string ResourcesFolder = "VFX";

        [Tooltip("Knows where each character is, so anchors can be found on whoever is currently shown.")]
        [SerializeField] private CharacterAssetConfig characters;

        [Tooltip("What a Camera Shake step moves: the BattleArea. The characters are UI, so moving the real camera would not shake them.")]
        [SerializeField] private RectTransform shakeTarget;

        private Dictionary<string, VfxResource> library;
        private bool warnedAboutOverlay;

        // Shakes that are running right now. Several can overlap: their movements add up.
        private sealed class ActiveShake
        {
            public VfxStep Step;
            public float Elapsed;
            public float NextJump;
            public Vector2 Offset;
        }

        private readonly List<ActiveShake> shakes = new();
        private Vector2 shakeRestPosition;
        private bool shaking;

        /// <summary>
        /// Starts a skill: plays its Start steps and returns a run that knows when the hit
        /// should land. Never returns null; a skill with no effect gives a run that does nothing.
        /// </summary>
        public VfxRun Begin(SkillResource skill, CharacterRole caster, CharacterRole target)
        {
            VfxRun run = CreateRun(skill, caster, target);
            Fire(run, VfxTrigger.Start);
            return run;
        }

        private VfxRun CreateRun(SkillResource skill, CharacterRole caster, CharacterRole target)
        {
            if (skill == null)
                return new VfxRun(this, new List<VfxStep>(), caster, target, 0f);

            // A skill with a VFX id uses that VFX.
            if (!string.IsNullOrWhiteSpace(skill.vfxId))
            {
                if (TryGetVfx(skill.vfxId, out VfxResource vfx))
                    return new VfxRun(this, vfx.steps, caster, target, vfx.impactDelay, vfx.replyDelay);

                Debug.LogWarning(
                    $"{skill.name}: no VFX with the id '{skill.vfxId}'. VFX assets must be in a Resources/VFX folder.", this);
                return new VfxRun(this, new List<VfxStep>(), caster, target, 0f);
            }

            // Otherwise the simple single-prefab effect on the skill itself.
            var steps = new List<VfxStep>();
            if (skill.vfxPrefab != null)
            {
                steps.Add(new VfxStep
                {
                    trigger = VfxTrigger.Start,
                    prefab = skill.vfxPrefab,
                    position = skill.placement == SkillPlacement.OnTarget ? VfxPosition.Target : VfxPosition.Caster,
                    anchor = skill.anchor,
                    offset = skill.positionOffset,
                    rotationMode = skill.overrideRotation ? VfxRotationMode.Custom : VfxRotationMode.Prefab,
                    rotation = skill.rotation,
                    scaleMode = skill.overrideScale ? VfxScaleMode.Custom : VfxScaleMode.Multiply,
                    scale = skill.scale,
                    customScale = skill.customScale,
                    lifetime = skill.lifetime,
                    sortingOrder = skill.sortingOrder
                });
            }

            return new VfxRun(this, steps, caster, target, steps.Count > 0 ? skill.impactDelay : 0f);
        }

        internal void Fire(VfxRun run, VfxTrigger trigger)
        {
            foreach (VfxStep step in run.Steps)
            {
                if (step == null || step.trigger != trigger)
                    continue;

                if (step.kind == VfxStepKind.Spawn && step.prefab == null)
                    continue;

                if (step.delay > 0f)
                    StartCoroutine(SpawnAfter(step, run));
                else
                    Spawn(step, run);
            }
        }

        private IEnumerator SpawnAfter(VfxStep step, VfxRun run)
        {
            yield return new WaitForSeconds(step.delay);
            Spawn(step, run);
        }

        private void Update()
        {
            if (shakeTarget == null)
                return;

            if (shakes.Count == 0)
            {
                if (shaking)
                {
                    shakeTarget.anchoredPosition = shakeRestPosition; // back to where it was
                    shaking = false;
                }

                return;
            }

            Vector2 total = Vector2.zero;
            for (int i = shakes.Count - 1; i >= 0; i--)
            {
                ActiveShake shake = shakes[i];
                shake.Elapsed += Time.deltaTime;

                if (shake.Elapsed >= shake.Step.shakeDuration)
                {
                    shakes.RemoveAt(i);
                    continue;
                }

                // Jump to a new random spot a set number of times a second.
                if (shake.Elapsed >= shake.NextJump)
                {
                    shake.NextJump = shake.Elapsed + 1f / Mathf.Max(1f, shake.Step.shakeFrequency);
                    float t = shake.Elapsed / shake.Step.shakeDuration;
                    float strength = shake.Step.shakeStrength * shake.Step.shakeCurve.Evaluate(t);
                    shake.Offset = Random.insideUnitCircle * strength;
                }

                total += shake.Offset;
            }

            shakeTarget.anchoredPosition = shakeRestPosition + total;
        }

        private void StartShake(VfxStep step)
        {
            if (shakeTarget == null)
            {
                Debug.LogWarning("A Camera Shake step needs the Shake Target (BattleArea) assigned on the VfxManager.", this);
                return;
            }

            if (!shaking)
            {
                shakeRestPosition = shakeTarget.anchoredPosition; // remembered so it can be put back
                shaking = true;
            }

            shakes.Add(new ActiveShake { Step = step });
        }

        private void OnDisable()
        {
            if (shaking && shakeTarget != null)
                shakeTarget.anchoredPosition = shakeRestPosition;

            shakes.Clear();
            shaking = false;
        }

        private void Spawn(VfxStep step, VfxRun run)
        {
            if (step.kind == VfxStepKind.CameraShake)
            {
                StartShake(step);
                return;
            }

            Transform point = FindAnchor(step.position, step.anchor, run);
            if (point == null)
            {
                Debug.LogWarning($"No anchor to spawn {step.prefab.name} on.", this);
                return;
            }

            Vector3 position = WorldPosition(point) + step.offset;
            Quaternion rotation = step.rotationMode == VfxRotationMode.Custom
                ? Quaternion.Euler(step.rotation)
                : step.prefab.transform.rotation;

            GameObject instance = Instantiate(step.prefab, position, rotation);

            Vector3 size = step.scaleMode switch
            {
                VfxScaleMode.Custom => step.customScale,
                VfxScaleMode.Multiply => step.prefab.transform.localScale * step.scale,
                _ => step.prefab.transform.localScale
            };

            if (step.parentToAnchor)
            {
                // The Canvas is tiny in the world, so keep the effect's world size when it becomes a child.
                instance.transform.SetParent(point, true);
                Vector3 parentScale = point.lossyScale;
                instance.transform.localScale = new Vector3(
                    size.x / SafeScale(parentScale.x), size.y / SafeScale(parentScale.y), size.z / SafeScale(parentScale.z));
            }
            else
            {
                instance.transform.localScale = size;
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sortingOrder = step.sortingOrder;

            if (step.travel)
            {
                Transform destination = FindAnchor(step.travelTo, step.travelAnchor, run);
                if (destination != null)
                    instance.AddComponent<VfxMover>().Begin(
                        () => WorldPosition(destination), step.travelDuration, step.faceTravelDirection, step.finishOnArrival);
            }

            Destroy(instance, step.lifetime);
        }

        private static float SafeScale(float value) => Mathf.Abs(value) < 1e-5f ? 1f : value;

        private Transform FindAnchor(VfxPosition position, VfxAnchorPoint anchor, VfxRun run)
        {
            if (characters == null)
                return null;

            CharacterRole side = position == VfxPosition.Target ? run.Target : run.Caster;
            VfxAnchors anchors = characters.GetAnchors(side);
            return anchors != null ? anchors.Get(anchor) : null;
        }

        private bool TryGetVfx(string id, out VfxResource vfx)
        {
            if (library == null)
            {
                library = new Dictionary<string, VfxResource>();
                foreach (VfxResource resource in Resources.LoadAll<VfxResource>(ResourcesFolder))
                {
                    if (library.ContainsKey(resource.Id))
                        Debug.LogWarning($"Two VFX share the id '{resource.Id}'. Ids must be unique.", resource);
                    else
                        library[resource.Id] = resource;
                }
            }

            return library.TryGetValue(id, out vfx);
        }

        private Vector3 WorldPosition(Transform point)
        {
            Canvas canvas = point.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;

            if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (!warnedAboutOverlay)
                {
                    warnedAboutOverlay = true;
                    Debug.LogWarning(
                        "The Canvas is Screen Space - Overlay, which is always drawn on top of particle effects, " +
                        "so they will be hidden behind the characters. Set the Canvas's Render Mode to " +
                        "Screen Space - Camera and assign the Main Camera.", this);
                }

                // Overlay anchors are in screen pixels: convert them to the world.
                Camera camera = Camera.main;
                if (camera != null)
                    return camera.ScreenToWorldPoint(new Vector3(point.position.x, point.position.y, Mathf.Abs(camera.transform.position.z)));
            }

            return point.position;
        }
    }
}
