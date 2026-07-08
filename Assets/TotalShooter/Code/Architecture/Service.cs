using System;
using System.Collections.Generic;

namespace Sadalmalik.TotalShooter.Architecture
{
    public static class Service
    {
        internal static Dictionary<Type, object> m_Instances = new Dictionary<Type, object>();
        
        public static T Get<T>()
        {
            return m_Instances.TryGetValue(typeof(T), out object instance) ? (T) instance : default(T);
        }

        public static void Add<T>() where T : new()
        {
            m_Instances.Add(typeof(T), new T());
        }
        
        public static void Add<T>(T instance)
        {
            m_Instances.Add(typeof(T), instance);
        }

        // Type I can be class or interface
        public static IEnumerable<I> GetAllOf<I>()
        {
            foreach (var (type, instance) in m_Instances)
            {
                if (typeof(I).IsAssignableFrom(type))
                    yield return (I) instance;
            }
        }

        public static void Remove<T>()
        {
            if (!m_Instances.TryGetValue(typeof(T), out var instance))
                return;
            if (instance is IDisposable disposable)
                disposable.Dispose();
            m_Instances.Remove(typeof(T));
        }

        public static void RemoveAll()
        {
            foreach (var instance in m_Instances.Values)
                if (instance is IDisposable disposable)
                    disposable.Dispose();
            m_Instances.Clear();
        }
    }
}