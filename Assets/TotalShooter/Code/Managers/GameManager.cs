using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Host-only "мозг матча": спавнит единый GameState и объект игрока на каждое подключение,
    // хостит матч-Lua (пока не подключено). Сам НЕ NetworkObject — клиентская копия не нужна, это
    // POCO-сервис. Тонкий: фазы/готовность/отсчёт решает матч-скрипт через KV-GameState, не C#.
    public class GameManager : System.IDisposable
    {
        private readonly GameState m_GameStatePrefab;
        private readonly NetworkObject m_PlayerPrefab;

        private GameState m_GameState;
        private bool m_Started;

        public GameState State => m_GameState;

        public GameManager(GameState gameStatePrefab, NetworkObject playerPrefab)
        {
            m_GameStatePrefab = gameStatePrefab;
            m_PlayerPrefab = playerPrefab;
        }

        // Вызывается после того, как SessionManager поднял хост (сессию через Sessions API).
        public void StartHost()
        {
            if (m_Started)
                return;
            m_Started = true;

            var ngo = NetworkManager.Singleton;

            m_GameState = Object.Instantiate(m_GameStatePrefab);
            m_GameState.NetworkObject.Spawn();

            ngo.OnClientConnectedCallback += SpawnPlayer;

            // Игроки, уже подключённые к моменту старта менеджера (как минимум сам хост).
            foreach (var clientId in ngo.ConnectedClientsIds)
                SpawnPlayer(clientId);
        }

        // Отключение отдельно не обрабатываем: NGO сам деспавнит объект игрока при дисконнекте.
        private void SpawnPlayer(ulong clientId)
        {
            var player = Object.Instantiate(m_PlayerPrefab);
            player.SpawnAsPlayerObject(clientId);
        }

        public void Dispose()
        {
            var ngo = NetworkManager.Singleton;
            if (ngo != null)
                ngo.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
}
