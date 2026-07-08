using System.Collections.Generic;
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
    }
}
