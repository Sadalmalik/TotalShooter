using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class EntityVariables : MonoBehaviour
    {
        private readonly Dictionary<string, object> m_Values = new();
        private readonly HashSet<string> m_ReplicatedKeys = new();

        public object this[string key]
        {
            get => m_Values.TryGetValue(key, out var value) ? value : null;
            set => m_Values[key] = value;
        }

        public void SetReplicated(string key, bool replicated)
        {
            if (replicated)
                m_ReplicatedKeys.Add(key);
            else
                m_ReplicatedKeys.Remove(key);
        }

        public bool IsReplicated(string key) => m_ReplicatedKeys.Contains(key);
    }
}
