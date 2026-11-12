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
        private bool face;
        private bool finish;

        public void Begin(Func<Vector3> getDestination, float seconds, bool faceDirection = false, bool finishOnArrival = false)
        {
            destination = getDestination;
            start = transform.position;
            duration = Mathf.Max(0.01f, seconds);
            elapsed = 0f;
            face = faceDirection;
            finish = finishOnArrival;
            Face(destination());
        }

        private void Update()
        {
            if (destination == null)
                return;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 target = destination();
            transform.position = Vector3.Lerp(start, target, t);
            Face(target);

            if (t >= 1f)
            {
                destination = null;
                if (finish)
                    Finish();
            }
        }

        // Points the effect's forward axis (+Z, how rockets are made) along the flight.
        private void Face(Vector3 target)
        {
            if (!face)
                return;

            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude > 1e-8f)
                transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        // Leaves the trails behind so they fade out where the effect ended, and removes the rest.
        private void Finish()
        {
            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
            {
                if (!system.gameObject.name.Contains("Trail"))
                    continue;

                system.Stop(false, ParticleSystemStopBehavior.StopEmitting); // existing particles fade, no new ones
                system.transform.SetParent(null, true);
                Destroy(system.gameObject, 2f);
            }

            Destroy(gameObject);
        }
    }
}
