using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Host-only "мозг матча": спавнит единый GameState, а на каждое подключение — контроллер и
    // пешку игрока, и раздаёт пешку контроллеру. Префабы берёт из GameConfig (не через конструктор).
    // Сам НЕ NetworkObject — это POCO-сервис. Тонкий: фазы/готовность решает матч-Lua через
    // KV-GameState, не хардкод C#.
    public class GameManager : System.IDisposable
    {
        private GameState m_GameState;
        private GameObject m_Environment;
        private bool m_Started;

        public GameState State => m_GameState;

        // Вызывается после того, как SessionManager поднял хост (сессию через Sessions API).
        public void StartHost()
        {
            if (m_Started)
                return;
            m_Started = true;

            var ngo = NetworkManager.Singleton;

            m_GameState = Object.Instantiate(GameConfig.Instance.GameStatePrefab);
            m_GameState.NetworkObject.Spawn();

            ngo.OnClientConnectedCallback += SpawnPlayer;

            // Игроки, уже подключённые к моменту старта менеджера (как минимум сам хост).
            foreach (var clientId in ngo.ConnectedClientsIds)
                SpawnPlayer(clientId);
        }

        // Контроллер + пешка на клиента (оба во владении клиента), пешку отдаём контроллеру.
        // Отключение отдельно не обрабатываем: NGO сам деспавнит объекты игрока при дисконнекте.
        private void SpawnPlayer(ulong clientId)
        {
            var config = GameConfig.Instance;

            var pawn = Object.Instantiate(config.GhostPawnPrefab);
            pawn.SpawnWithOwnership(clientId);

            var controller = Object.Instantiate(config.PlayerControllerPrefab);
            controller.SpawnAsPlayerObject(clientId);
            controller.GetComponent<PlayerNetwork>().AssignPawn(pawn);
        }

        // Окружение — не сетевое: каждый инстанс (хост и клиенты) поднимает своё локально при входе.
        public void SpawnLocalEnvironment()
        {
            if (m_Environment != null)
                return;

            var prefab = GameConfig.Instance.EnvironmentPrefab;
            if (prefab != null)
                m_Environment = Object.Instantiate(prefab);
        }

        public void Dispose()
        {
            var ngo = NetworkManager.Singleton;
            if (ngo != null)
                ngo.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
}
