using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent<T>(out T component))
                return component;
            return gameObject.AddComponent<T>();
        }
    }
}
