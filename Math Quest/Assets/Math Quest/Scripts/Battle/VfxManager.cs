using MathQuest.Data;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// Spawns the effect of a skill at an anchor point of a character, and removes it again
    /// after its lifetime. The battle only asks "play this skill, caster to target"; where
    /// the effect goes and for how long is decided by the SkillResource.
    /// </summary>
    public sealed class VfxManager : MonoBehaviour
    {
        [Tooltip("Knows where each character is, so anchors can be found on whoever is currently shown.")]
        [SerializeField] private CharacterAssetConfig characters;

        private bool warnedAboutOverlay;

        /// <summary>
        /// Plays the skill's effect. Returns how many seconds until the hit should land, so the
        /// caller can wait that long before taking a heart. Returns 0 when there is no effect.
        /// </summary>
        public float Play(SkillResource skill, CharacterRole caster, CharacterRole target)
        {
            if (skill == null || skill.vfxPrefab == null || characters == null)
                return 0f;

            CharacterRole side = skill.placement == SkillPlacement.OnTarget ? target : caster;
            VfxAnchors anchors = characters.GetAnchors(side);
            if (anchors == null)
            {
                Debug.LogWarning($"{side} has no VfxAnchors (the AnchorVFX prefab) to spawn {skill.name} on.", this);
                return 0f;
            }

            Transform point = anchors.Get(skill.anchor);
            Vector3 position = WorldPosition(point);

            // Effects live in the world, not under the Canvas, so the Canvas's scale does not shrink them.
            GameObject instance = Instantiate(skill.vfxPrefab, position, Quaternion.identity);
            instance.transform.localScale = skill.vfxPrefab.transform.localScale * skill.scale;

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                renderer.sortingOrder = skill.sortingOrder;

            Destroy(instance, skill.lifetime);
            return skill.impactDelay;
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
