using System.Collections.Generic;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Ядро мира: центральный реестр всех Entity по EntityId. На него завязано всё, что
    // адресует объекты по id — репликация, Spawner, sandbox-редактирование, SceneConverter.
    // Резолвинг прототипов (data.json) и загрузка/сохранение мира (world.json) — отдельные
    // куски, добавляются следующими.
    public class WorldManager : MonoBehaviour
    {
        // EntityId == 0 — "не присвоен"; реальные id начинаются с 1.
        public const int NoId = 0;

        public static WorldManager Instance { get; private set; }

        private readonly Dictionary<int, Entity> m_Entities = new();
        private int m_NextId = 1;

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
    }
}
