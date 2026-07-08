using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class Entity : MonoBehaviour
    {
        public int EntityId;
        public Controller controller;

        // Имя контента (префаб/прототип), из которого Entity создан — нужно для round-trip
        // сохранения (переинстанцировать базу, поверх наложить сохранённые оверрайды).
        public string Proto;

        public object this[string key]
        {
            get => gameObject.GetComponent<EntityVariables>()?[key];
            set => gameObject.GetOrAddComponent<EntityVariables>()[key] = value;
        }
    }
}