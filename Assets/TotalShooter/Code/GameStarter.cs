using Sadalmalik.TotalShooter.Architecture;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Черновой bootstrap для локального теста в Play Mode: регистрирует менеджеры-сервисы и
    // поднимает окружение/пешку/камеру/контроллер. Полноценный жизненный цикл сессии
    // (загрузка мира, фазы матча, сеть) — отдельная задача, см. code.md "Жизненный цикл сессии".
    public class GameStarter : MonoBehaviour
    {
        [SerializeField] private GameObject m_EnvironmentPrefab;
        [SerializeField] private Entity m_GhostPrefab;
        [SerializeField] private CameraOperator m_CameraOperatorPrefab;
        [SerializeField] private PlayerController m_PlayerControllerPrefab;

        private void Start()
        {
            // Менеджеры — POCO-сервисы, достаются везде через Service.Get<T>(). NetworkManager
            // сам поднимает Unity Services лениво при создании/входе в сессию, здесь не инитим.
            Service.Add(new WorldManager());
            Service.Add(new NetworkManager());

            // Локальный тест: оператор камеры создаётся вместе с контроллером; пешка (призрак) —
            // отдельно, контроллер её possess'ит.
            if (m_EnvironmentPrefab != null)
                Instantiate(m_EnvironmentPrefab);

            var pawn = Instantiate(m_GhostPrefab);
            var camera = Instantiate(m_CameraOperatorPrefab);
            var controller = Instantiate(m_PlayerControllerPrefab);

            controller.Setup(camera, pawn);
        }

        // Чистим реестр сервисов на выходе — статик переживает выход из Play Mode (domain reload
        // может быть отключён), иначе в следующий заход потащим мёртвые ссылки.
        private void OnApplicationQuit()
        {
            Service.RemoveAll();
        }
    }
}
