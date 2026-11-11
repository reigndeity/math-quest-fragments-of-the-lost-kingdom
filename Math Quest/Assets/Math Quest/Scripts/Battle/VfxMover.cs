using System;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>Flies an effect from where it is to a point on a character. Added by the VfxManager.</summary>
    public sealed class VfxMover : MonoBehaviour
    {
        private Func<Vector3> destination;
        private Vector3 start;
        private float duration;
        private float elapsed;

        public void Begin(Func<Vector3> getDestination, float seconds)
        {
            destination = getDestination;
            start = transform.position;
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
        }

        private void Update()
        {
            if (destination == null)
                return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, destination(), t);

            if (t >= 1f)
                destination = null;
        }
    }
}
