using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class Entity : MonoBehaviour
    {
        public int EntityId;
        public Controller controller;

        // Логическое имя объекта (data), сохраняется в world.json. При инстанцировании из мира
        // проставляется в gameObject.name, чтобы объекты на сцене были узнаваемы. Не путать с
        // унаследованным gameObject.name — это персистентный источник имени, тот — рантайм-ярлык.
        public string Name;

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