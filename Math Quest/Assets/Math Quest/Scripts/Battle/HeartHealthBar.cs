using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// Drives hearts you placed in the scene yourself. Put it on the parent of the
    /// HealthHearts instances; every <see cref="HeartView"/> below it counts as 1 HP, in
    /// hierarchy order. Damage turns off the last filled heart's Heart Fill.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeartHealthBar : MonoBehaviour
    {
        private HeartView[] hearts;

        public int Current { get; private set; }
        public bool IsDefeated => Current <= 0;

        private void Awake()
        {
            hearts = GetComponentsInChildren<HeartView>(true);
            Refill();
        }

        public void Damage(int amount = 1)
        {
            SetHealth(Current - amount);
        }

        public void Refill()
        {
            SetHealth(hearts.Length);
        }

        public void SetHealth(int value)
        {
            Current = Mathf.Clamp(value, 0, hearts.Length);
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].SetFilled(i < Current);
        }
    }
}
