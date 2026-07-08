using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Plain C# class, not a MonoBehaviour — abilities are "cold" logic (see
    // "Массовые юниты: сначала ООП" in .claude/architecture-core.md), owned by whatever
    // holds them (character, weapon), resolved by name through the content registry later.
    public abstract class Ability
    {
        private float m_CooldownRemaining;

        public Entity Owner { get; private set; }

        public abstract float Cooldown { get; }

        public bool IsReady => m_CooldownRemaining <= 0;

        public void Bind(Entity owner)
        {
            Owner = owner;
        }

        public void Tick(float deltaTime)
        {
            if (m_CooldownRemaining > 0)
                m_CooldownRemaining = Mathf.Max(0, m_CooldownRemaining - deltaTime);
        }

        public bool TryActivate()
        {
            if (!IsReady)
                return false;

            m_CooldownRemaining = Cooldown;
            OnActivate();
            return true;
        }

        protected abstract void OnActivate();
    }
}
