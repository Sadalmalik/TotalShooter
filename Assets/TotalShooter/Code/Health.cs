using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public enum DamageType
    {
        Generic,
    }

    public class Health : MonoBehaviour
    {
        [SerializeField] private float m_Current = 100;
        [SerializeField] private float m_Max = 100;

        private readonly Dictionary<DamageType, float> m_Resistances = new();

        public float Current => m_Current;
        public float Max => m_Max;

        public event Action<int> OnDeath;

        public void SetResistance(DamageType type, float multiplier)
        {
            m_Resistances[type] = multiplier;
        }

        public void ApplyDamage(float amount, DamageType type, int source)
        {
            if (m_Current <= 0)
                return;

            var resistance = m_Resistances.TryGetValue(type, out var value) ? value : 1f;
            m_Current = Mathf.Max(0, m_Current - amount * resistance);

            if (m_Current <= 0)
                OnDeath?.Invoke(source);
        }
    }
}
