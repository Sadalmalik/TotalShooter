using UnityEngine;

namespace Sadalmalik.TotalShooter.Architecture
{
    // Базовый ScriptableObject-синглтон: лениво грузит единственный ассет из Resources по имени
    // типа. Ассет должен лежать в любой папке Resources/ и называться как класс (напр. GameConfig).
    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : SingletonScriptableObject<T>
    {
        private static T s_Instance;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = Resources.Load<T>(typeof(T).Name);
                if (s_Instance == null)
                    Debug.LogError($"{typeof(T).Name} not found in a Resources folder");
                return s_Instance;
            }
        }
    }
}
