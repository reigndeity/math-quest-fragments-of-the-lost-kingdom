using MathQuest.Data;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// On the AnchorVFX prefab inside every character. Skills ask it for a point on the
    /// character (Root, Center or Top) to spawn their effect at.
    /// </summary>
    public sealed class VfxAnchors : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform center;
        [SerializeField] private Transform top;

        public Transform Get(VfxAnchorPoint point)
        {
            Transform found = point switch
            {
                VfxAnchorPoint.Root => root,
                VfxAnchorPoint.Top => top,
                _ => center
            };

            return found != null ? found : transform;
        }
    }
}
