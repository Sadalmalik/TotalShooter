using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class Entity : MonoBehaviour
    {
        public int EntityId;
        public Controller controller;

        public object this[string key]
        {
            get => gameObject.GetComponent<EntityVariables>()?[key];
            set => gameObject.GetOrAddComponent<EntityVariables>()[key] = value;
        }
    }
}