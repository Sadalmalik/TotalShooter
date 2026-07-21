using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Накладывает JSON-оверрайды на компоненты Entity по имени: { "Health": { "Max": 40 } }.
    // Общий примитив для резолвинга прототипов (data.json) и загрузки инстансов (world.json).
    // Ключ записи = имя типа компонента, значение = объект { поле: значение }.
    public static class ComponentOverrides
    {
        // Ключи записи, которые не являются оверрайдами компонентов (обрабатываются загрузчиком
        // отдельно): служебные поля и Transform (у него Unity-типы, задаётся не рефлексией).
        private static readonly HashSet<string> ReservedKeys = new()
        {
            "name", "prototype", "prefab", // data.json
            "Proto", "EntityId", "Parent", "Transform", "Name", // world.json
        };

        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly Dictionary<string, Type> s_ComponentTypes = new();

        public static void Apply(Entity entity, JObject source)
        {
            foreach (var property in source.Properties())
            {
                if (ReservedKeys.Contains(property.Name))
                    continue;

                if (property.Value is not JObject fields)
                {
                    Debug.LogError($"Override '{property.Name}' on '{entity.name}' must be an object", entity);
                    continue;
                }

                var type = ResolveComponentType(property.Name);
                if (type == null)
                {
                    Debug.LogError($"Unknown component type '{property.Name}' on '{entity.name}'", entity);
                    continue;
                }

                var component = entity.GetComponent(type);
                if (component == null)
                    component = entity.gameObject.AddComponent(type);

                ApplyFields(component, fields);
            }
        }

        private static void ApplyFields(Component component, JObject fields)
        {
            var type = component.GetType();
            foreach (var field in fields.Properties())
            {
                if (!TryWrite(component, type, field.Name, field.Value))
                    Debug.LogError($"'{type.Name}' has no writable field/property '{field.Name}'", component);
            }
        }

        // Обратная операция к Apply: дампит игровые компоненты Entity в JObject { "Health": {...} }.
        // Берёт только компоненты из нашей сборки (не Unity-встроенные), кроме самого Entity и
        // контроллеров (те — рантайм, не данные мира). Ключи полей — чистые имена (m_Max → "Max").
        public static JObject Dump(Entity entity)
        {
            var result = new JObject();

            foreach (var component in entity.GetComponents<MonoBehaviour>())
            {
                if (component == null)
                    continue;

                var type = component.GetType();
                if (type == typeof(Entity) || component is Controller)
                    continue;
                if (type.Assembly != typeof(Entity).Assembly)
                    continue;

                // Пишем запись даже без полей: само наличие компонента — это состояние (маркеры
                // вроде SpawnPoint/Item без полей должны переживать сохранение/загрузку как "{}").
                result[type.Name] = DumpFields(component, type);
            }

            // Встроенный Light — не из нашей сборки, поэтому в цикл выше не попадает. Сохраняем
            // точечно (только если есть), чтобы свет можно было править из песочницы. Читается
            // обратно общим Apply (резолвит UnityEngine.Light по имени и пишет свойства). Без света
            // ключ не заводим. Набор свойств — курируемый (у Light десятки, многие read-only).
            if (entity.TryGetComponent<Light>(out var light))
                result["Light"] = DumpLight(light);

            return result;
        }

        private static JObject DumpLight(Light light)
        {
            var c = light.color;
            return new JObject
            {
                ["type"] = light.type.ToString(),
                ["color"] = new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a },
                ["intensity"] = light.intensity,
                ["range"] = light.range,
                ["spotAngle"] = light.spotAngle,
                ["shadows"] = light.shadows.ToString(),
            };
        }

        private static JObject DumpFields(object target, Type type)
        {
            var result = new JObject();

            foreach (var field in type.GetFields(MemberFlags))
            {
                // Только то, что сериализует Unity (public или [SerializeField]), и не ссылки на
                // Unity-объекты (их сохраняем по имени контента, а не как граф ссылок).
                var serialized = field.IsPublic || field.IsDefined(typeof(SerializeField));
                if (!serialized || field.IsDefined(typeof(NonSerializedAttribute)))
                    continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    continue;

                try
                {
                    result[StripPrefix(field.Name)] = JToken.FromObject(field.GetValue(target));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to dump '{type.Name}.{field.Name}': {e.Message}");
                }
            }

            return result;
        }

        private static string StripPrefix(string name)
        {
            return name.StartsWith("m_") ? name.Substring(2) : name;
        }

        // JSON-ключ "Max" мапится на C#-член в порядке: публичное поле Max → приватное
        // сериализуемое поле m_Max → property Max с сеттером. Так чистые имена в JSON ложатся
        // и на публичные поля, и на приватные `m_`-поля стиля проекта.
        private static bool TryWrite(object target, Type type, string name, JToken value)
        {
            var field = type.GetField(name, MemberFlags) ?? type.GetField("m_" + name, MemberFlags);
            if (field != null)
            {
                Assign(() => field.SetValue(target, value.ToObject(field.FieldType)), type, name, target);
                return true;
            }

            var property = type.GetProperty(name, MemberFlags);
            if (property is { CanWrite: true })
            {
                Assign(() => property.SetValue(target, value.ToObject(property.PropertyType)), type, name, target);
                return true;
            }

            return false;
        }

        private static void Assign(Action set, Type type, string name, object target)
        {
            try
            {
                set();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set '{type.Name}.{name}': {e.Message}", target as UnityEngine.Object);
            }
        }

        // Резолвит имя компонента в System.Type среди всех загруженных сборок (кэш). Совпадение
        // по короткому имени типа среди наследников Component.
        private static Type ResolveComponentType(string name)
        {
            if (s_ComponentTypes.TryGetValue(name, out var cached))
                return cached;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                foreach (var type in types)
                {
                    if (type != null && type.Name == name && typeof(Component).IsAssignableFrom(type))
                    {
                        s_ComponentTypes[name] = type;
                        return type;
                    }
                }
            }

            s_ComponentTypes[name] = null;
            return null;
        }
    }
}
