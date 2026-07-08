using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Ядро мира: центральный реестр всех Entity по EntityId + резолвинг прототипов контента
    // (data.json). Загрузка/сохранение мира (world.json) с файловой IO — добавляется следующим
    // куском. На реестр завязано всё, что адресует объекты по id — репликация, Spawner,
    // sandbox-редактирование, SceneConverter.
    public class WorldManager : MonoBehaviour
    {
        // EntityId == 0 — "не присвоен"; реальные id начинаются с 1.
        public const int NoId = 0;

        public static WorldManager Instance { get; private set; }

        private readonly Dictionary<int, Entity> m_Entities = new();
        private int m_NextId = 1;

        // Локальные для мира определения контента (data.json): имя → полная JSON-запись
        // (name/prototype/prefab + оверрайды компонентов).
        private readonly Dictionary<string, JObject> m_Definitions = new();

        public IReadOnlyDictionary<int, Entity> Entities => m_Entities;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public Entity Get(int entityId)
        {
            return m_Entities.TryGetValue(entityId, out var entity) ? entity : null;
        }

        // Регистрирует Entity. Если id не присвоен (0) — выдаёт новый. Если присвоен (загрузка
        // из world.json) — использует его и двигает счётчик за него, чтобы будущие авто-id не
        // столкнулись с уже занятыми.
        public void Register(Entity entity)
        {
            if (entity.EntityId == NoId)
                entity.EntityId = m_NextId++;
            else if (entity.EntityId >= m_NextId)
                m_NextId = entity.EntityId + 1;

            if (m_Entities.TryGetValue(entity.EntityId, out var existing) && existing != entity)
            {
                Debug.LogError($"EntityId {entity.EntityId} already registered to '{existing.name}', " +
                               $"cannot register '{entity.name}'", entity);
                return;
            }

            m_Entities[entity.EntityId] = entity;
        }

        public void Unregister(Entity entity)
        {
            if (m_Entities.TryGetValue(entity.EntityId, out var registered) && registered == entity)
                m_Entities.Remove(entity.EntityId);
        }

#region Реестр контента (data.json + резолвинг прототипов)

        // Разбирает содержимое data.json (JSON-массив записей) в реестр определений. Само чтение
        // файла Worlds/<name>/data.json — на стороне загрузчика мира (следующий кусок).
        public void LoadData(string dataJson)
        {
            m_Definitions.Clear();

            if (string.IsNullOrWhiteSpace(dataJson))
                return;

            JArray entries;
            try
            {
                entries = JArray.Parse(dataJson);
            }
            catch (JsonException e)
            {
                Debug.LogError($"Failed to parse data.json: {e.Message}");
                return;
            }

            foreach (var token in entries)
            {
                if (token is not JObject entry)
                    continue;

                var name = entry.Value<string>("name");
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogError("data.json entry has no 'name'");
                    continue;
                }

                // Запись с уже существующим именем заменяет определение глобально для мира
                // (Factorio-style override) — последняя запись побеждает.
                m_Definitions[name] = entry;
            }
        }

        // Резолвит имя контента в готовый инстанс Entity: инстанцирует базу (префаб или другой
        // прототип по цепочке) и накладывает оверрайды. Инстанс НЕ регистрируется здесь — id
        // присваивает вызывающий (Spawner/загрузчик мира), когда объект становится инстансом мира.
        public Entity ResolvePrototype(string name)
        {
            return Resolve(name, new HashSet<string>());
        }

        private Entity Resolve(string name, HashSet<string> visited)
        {
            if (!visited.Add(name))
            {
                Debug.LogError($"Cycle in prototype chain at '{name}'");
                return null;
            }

            // Порядок поиска: локальный data.json → (моды позже) → префаб как базовый ассет.
            if (!m_Definitions.TryGetValue(name, out var definition))
                return LoadPrefabInstance(name);

            var prototype = definition.Value<string>("prototype");
            var prefab = definition.Value<string>("prefab");

            Entity instance = prototype != null
                ? Resolve(prototype, visited)
                : LoadPrefabInstance(prefab ?? name);

            if (instance == null)
                return null;

            ComponentOverrides.Apply(instance, definition);
            return instance;
        }

        private Entity LoadPrefabInstance(string path)
        {
            // Пока через Resources; переход на AssetBundle — отдельно (см. architecture-core.md).
            var prefab = Resources.Load<Entity>(path);
            if (prefab == null)
            {
                Debug.LogError($"Prefab '{path}' not found (Resources)");
                return null;
            }

            return Instantiate(prefab);
        }

#endregion

#region Загрузка мира (world.json)

        // Текущая версия схемы файлов мира. Несовпадение — явный отказ загрузки (миграции пока
        // не строим, см. architecture-core.md "Версионирование схемы").
        public const int SchemaVersion = 1;

        // Папка Worlds/ рядом с исполняемым файлом (в редакторе — рядом с папкой Assets).
        public static string WorldsRoot =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Worlds");

        // Загружает мир из папки Worlds/<worldName>/ (data.json + world.json).
        public bool LoadWorld(string worldName)
        {
            var folder = Path.Combine(WorldsRoot, worldName);
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"World folder not found: {folder}");
                return false;
            }

            var dataPath = Path.Combine(folder, "data.json");
            LoadData(File.Exists(dataPath) ? File.ReadAllText(dataPath) : null);

            var worldPath = Path.Combine(folder, "world.json");
            if (!File.Exists(worldPath))
            {
                Debug.LogError($"world.json not found in {folder}");
                return false;
            }

            return LoadWorldJson(File.ReadAllText(worldPath));
        }

        public bool LoadWorldJson(string worldJson)
        {
            JObject root;
            try
            {
                root = JObject.Parse(worldJson);
            }
            catch (JsonException e)
            {
                Debug.LogError($"Failed to parse world.json: {e.Message}");
                return false;
            }

            var version = root.Value<int?>("schemaVersion");
            if (version != SchemaVersion)
            {
                Debug.LogError($"world.json schemaVersion {version} != expected {SchemaVersion}; refusing to load");
                return false;
            }

            if (root["entities"] is not JArray entities)
            {
                Debug.LogError("world.json has no 'entities' array");
                return false;
            }

            // Проход 1: инстанцировать все записи плоско (id + компоненты + Transform), запомнив
            // их вместе с записью для линковки родителей.
            var created = new List<(Entity entity, JObject entry)>();
            foreach (var token in entities)
            {
                if (token is not JObject entry)
                    continue;

                var entity = InstantiateEntry(entry);
                if (entity != null)
                    created.Add((entity, entry));
            }

            // Проход 2: связать родителей по EntityId. Битый/несуществующий Parent — как отсутствие
            // родителя; линковка, создающая цикл — отклоняется.
            foreach (var (entity, entry) in created)
            {
                var parentId = entry.Value<int?>("Parent");
                if (parentId == null)
                    continue;

                var parent = Get(parentId.Value);
                if (parent == null)
                {
                    Debug.LogWarning($"Entity {entity.EntityId}: parent {parentId} not found, treating as root", entity);
                    continue;
                }

                if (WouldCycle(entity.transform, parent.transform))
                {
                    Debug.LogError($"Entity {entity.EntityId}: parenting to {parentId} would create a cycle", entity);
                    continue;
                }

                entity.transform.SetParent(parent.transform, false);
            }

            return true;
        }

        private Entity InstantiateEntry(JObject entry)
        {
            // "Proto" резолвится через ResolvePrototype — значение может быть как именем префаба,
            // так и именем прототипа из data.json (единая точка резолвинга). Без "Proto" —
            // голая Entity, целиком описанная инлайновыми оверрайдами компонентов.
            var proto = entry.Value<string>("Proto");
            var entity = proto != null
                ? ResolvePrototype(proto)
                : new GameObject("Entity").AddComponent<Entity>();

            if (entity == null)
                return null;

            entity.Proto = proto;
            entity.EntityId = entry.Value<int?>("EntityId") ?? NoId;
            Register(entity);

            ComponentOverrides.Apply(entity, entry);
            ApplyTransform(entity.transform, entry["Transform"] as JObject);
            return entity;
        }

        private static void ApplyTransform(Transform transform, JObject t)
        {
            if (t == null)
                return;

            if (t["Position"] is { } position)
                transform.localPosition = ToVector3(position);
            if (t["Rotation"] is { } rotation)
                transform.localEulerAngles = ToVector3(rotation);
            if (t["Scale"] is { } scale)
                transform.localScale = ToVector3(scale);
        }

        private static Vector3 ToVector3(JToken token)
        {
            var v = token.ToObject<float[]>();
            return v is { Length: >= 3 } ? new Vector3(v[0], v[1], v[2]) : Vector3.zero;
        }

        private static bool WouldCycle(Transform child, Transform parent)
        {
            for (var t = parent; t != null; t = t.parent)
                if (t == child)
                    return true;
            return false;
        }

#endregion

#region Сохранение мира (world.json)

        // Пишет текущее состояние мира в Worlds/<worldName>/world.json. data.json не трогаем —
        // это авторские определения контента, не состояние (сохраняем только инстансы).
        public bool SaveWorld(string worldName)
        {
            var folder = Path.Combine(WorldsRoot, worldName);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "world.json"), SaveWorldJson());
            return true;
        }

        public string SaveWorldJson()
        {
            var entities = new JArray();

            foreach (var entity in SortedById())
            {
                var entry = new JObject { ["EntityId"] = entity.EntityId };

                if (!string.IsNullOrEmpty(entity.Proto))
                    entry["Proto"] = entity.Proto;

                var parent = entity.transform.parent != null
                    ? entity.transform.parent.GetComponent<Entity>()
                    : null;
                if (parent != null)
                    entry["Parent"] = parent.EntityId;

                entry["Transform"] = DumpTransform(entity.transform);
                entry.Merge(ComponentOverrides.Dump(entity));

                entities.Add(entry);
            }

            var root = new JObject
            {
                ["schemaVersion"] = SchemaVersion,
                ["entities"] = entities,
            };
            return root.ToString(Formatting.Indented);
        }

        // Стабильный порядок по EntityId — чтобы дифф сохранений был читаемым.
        private List<Entity> SortedById()
        {
            var list = new List<Entity>(m_Entities.Values);
            list.Sort((a, b) => a.EntityId.CompareTo(b.EntityId));
            return list;
        }

        private static JObject DumpTransform(Transform t)
        {
            return new JObject
            {
                ["Position"] = ToArray(t.localPosition),
                ["Rotation"] = ToArray(t.localEulerAngles),
                ["Scale"] = ToArray(t.localScale),
            };
        }

        private static JArray ToArray(Vector3 v)
        {
            return new JArray(v.x, v.y, v.z);
        }

#endregion
    }
}
