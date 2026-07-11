using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Bootstrap стартовой сцены: регистрирует менеджеры-сервисы и запускает UI-меню. Если UI не
    // назначен — падает в локальный тест движения (призрак+камера+контроллер), как раньше.
    public class GameStarter : MonoBehaviour
    {
        [Header("Меню")]
        [SerializeField] private UIManager m_UI;

        [Header("Сессия (спавнит GameManager)")]
        [SerializeField] private GameState m_GameStatePrefab;
        [SerializeField] private NetworkObject m_PlayerPrefab;

        [Header("Локальный тест (fallback, если UI не задан)")]
        [SerializeField] private GameObject m_EnvironmentPrefab;
        [SerializeField] private Entity m_GhostPrefab;
        [SerializeField] private CameraOperator m_CameraOperatorPrefab;
        [SerializeField] private PlayerController m_PlayerControllerPrefab;

        private void Start()
        {
            // Менеджеры — POCO-сервисы, достаются везде через Service.Get<T>(). SessionManager
            // сам поднимает Unity Services лениво при создании/входе в сессию, здесь не инитим.
            Service.Add(new WorldManager());
            Service.Add(new SessionManager());
            Service.Add(new GameManager(m_GameStatePrefab, m_PlayerPrefab));

            if (m_UI != null)
            {
                m_UI.ShowMainMenu();
                return;
            }

            RunLocalMovementTest();
        }

        // Оператор камеры создаётся вместе с контроллером; пешка (призрак) — отдельно, контроллер
        // её possess'ит. Оставлено для быстрой проверки движения без сети/меню.
        private void RunLocalMovementTest()
        {
            if (m_GhostPrefab == null)
                return;

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
