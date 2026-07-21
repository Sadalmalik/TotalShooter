using System.Collections;
using System.Collections.Generic;
using Sadalmalik.TotalShooter.Architecture;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Спавнер юнитов на Entity. Имена типов резолвит через реестр контента
    // (WorldManager.ResolvePrototype), не прямые префаб-ссылки — иначе локальные оверрайды мира
    // (data.json) тихо не применятся. Тикается корутинами (не Update на каждом объекте — по
    // гайдлайну архитектуры; спавнеров немного). Спавнит ЛОКАЛЬНЫЕ Entity и регистрирует их в
    // реестре; сетевая синхронизация мобов — будущий канал репликации Entity (см. TODO), пока
    // Spawner проверяется в одиночке/на хосте.
    public class Spawner : MonoBehaviour
    {
        [SerializeField] private List<string> m_Tags = new();
        [SerializeField] private List<string> m_Units = new();
        [SerializeField] private float m_SpawnCooldown = 1f;
        [SerializeField] private bool m_AutoSpawn;
        [SerializeField] private int m_AutoSpawnLimit; // 0 — без лимита (автоспавн не сбрасывается сам)

        // Теги — чтобы AIDirector/зональные скрипты выбирали нужные спавнеры (зона/сложность/тип).
        public IReadOnlyList<string> Tags => m_Tags;

        private int m_AutoSpawned;

        // Автоспавн стартуем в Start, а не в OnEnable: при загрузке мира ComponentOverrides сначала
        // делает AddComponent (→ OnEnable, поля ещё дефолтные), и только потом применяет поля из
        // JSON. Start вызывается после того, как синхронный LoadWorld всё это завершил.
        private void Start()
        {
            if (m_AutoSpawn)
                StartCoroutine(AutoSpawnLoop());
        }

        // Ручная команда (AIDirector/зональный скрипт/Lua): заспавнить count юнитов с интервалом
        // interval секунд. Не связана с автоспавн-лимитом — отдельный поток спавна.
        public void Spawn(int count, float interval)
        {
            if (count > 0)
                StartCoroutine(SpawnBurst(count, interval));
        }

        private IEnumerator SpawnBurst(int count, float interval)
        {
            for (var i = 0; i < count; i++)
            {
                SpawnOne();
                if (i < count - 1 && interval > 0)
                    yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator AutoSpawnLoop()
        {
            while (m_AutoSpawn)
            {
                yield return new WaitForSeconds(m_SpawnCooldown);
                if (!m_AutoSpawn)
                    yield break;

                SpawnOne();

                if (m_AutoSpawnLimit > 0 && ++m_AutoSpawned >= m_AutoSpawnLimit)
                    m_AutoSpawn = false; // лимит достигнут — автоспавн выключается сам
            }
        }

        // Один юнит: случайный тип из Units, резолв через реестр, позиция/поворот = у спавнера,
        // регистрация в реестре (получает EntityId). Proto проставляем, чтобы моб корректно
        // сохранялся (переинстанцируется по имени при загрузке).
        private Entity SpawnOne()
        {
            if (m_Units.Count == 0)
            {
                Debug.LogWarning($"Spawner '{name}': список Units пуст", this);
                return null;
            }

            var unitName = m_Units[Random.Range(0, m_Units.Count)];
            var world = Service.Get<WorldManager>();

            var entity = world.ResolvePrototype(unitName);
            if (entity == null)
                return null; // ResolvePrototype уже залогировал причину (неизвестное имя/цикл)

            entity.Proto = unitName;
            entity.transform.SetPositionAndRotation(transform.position, transform.rotation);
            world.Register(entity);
            return entity;
        }
    }
}
