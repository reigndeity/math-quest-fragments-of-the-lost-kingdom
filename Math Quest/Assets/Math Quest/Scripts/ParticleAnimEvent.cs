using UnityEngine;

namespace MathQuest
{
    public class ParticleAnimEvent : MonoBehaviour
    {
        [SerializeField] ParticleSystem particleSystem;

        public void PlayParticle()
        {
            particleSystem.Play();
        }
    }
}
