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
            "name", "prototype", "prefab", "EntityId", "Parent", "Transform",
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
