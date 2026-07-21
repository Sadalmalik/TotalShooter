using System.Collections.Generic;
using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Host-only "мозг матча": грузит мир локально, спавнит единый GameState, а на каждое подключение
    // — контроллер и пешку игрока (в точке спавна мира), и раздаёт пешку контроллеру. Префабы берёт
    // из GameConfig. Сам НЕ NetworkObject — это POCO-сервис. Тонкий: фазы/готовность решает матч-Lua
    // через KV-GameState, не хардкод C#.
    //
    // Мир — данные на диске (Worlds/<name>/), не реплицируются (скачивание отложено). Хост знает имя
    // из меню; клиенту оно приходит через GameState["world"], и клиент грузит свою локальную копию.
    public class GameManager : System.IDisposable
    {
        // Ключ GameState, под которым хост публикует имя загруженного мира для клиентов.
        public const string WorldKey = "world";

        private GameState m_GameState;
        private int m_NextSpawn;
        private bool m_Started;
        private bool m_WorldLoaded;

        public GameState State => m_GameState;

        // Хост: грузит мир локально, поднимает GameState (публикует имя мира), спавнит игроков.
        // Вызывается после того, как SessionManager поднял хост (сессию через Sessions API).
        public void StartHost(string worldName)
        {
            if (m_Started)
                return;
            m_Started = true;

            LoadWorldLocal(worldName);

            var ngo = NetworkManager.Singleton;

            m_GameState = Object.Instantiate(GameConfig.Instance.GameStatePrefab);
            m_GameState.NetworkObject.Spawn();
            m_GameState[WorldKey] = worldName;

            ngo.OnClientConnectedCallback += SpawnPlayer;

            // Игроки, уже подключённые к моменту старта менеджера (как минимум сам хост).
            foreach (var clientId in ngo.ConnectedClientsIds)
                SpawnPlayer(clientId);
        }

        // Клиент: как только реплицируется GameState — читает имя мира и грузит его локальную копию.
        // Пешки клиенту спавнит хост; здесь только своя копия геометрии/контента мира.
        public void StartClient()
        {
            if (m_Started)
                return;
            m_Started = true;

            if (GameState.Current != null)
                OnGameStateSpawned(GameState.Current);
            else
                GameState.Spawned += OnGameStateSpawned;
        }

        private void OnGameStateSpawned(GameState state)
        {
            m_GameState = state;
            state.Changed += TryLoadWorldFromState;
            TryLoadWorldFromState(); // на случай, если ключ уже пришёл вместе со спавном
        }

        private void TryLoadWorldFromState()
        {
            if (m_WorldLoaded)
                return;
            if (m_GameState[WorldKey] is string worldName && !string.IsNullOrEmpty(worldName))
                LoadWorldLocal(worldName);
        }

        private void LoadWorldLocal(string worldName)
        {
            if (m_WorldLoaded)
                return;
            m_WorldLoaded = true;

            if (!Service.Get<WorldManager>().LoadWorld(worldName))
                Debug.LogError($"GameManager: не удалось загрузить мир '{worldName}'");
        }

        // Контроллер + пешка на клиента (оба во владении клиента), пешку отдаём контроллеру.
        // Отключение отдельно не обрабатываем: NGO сам деспавнит объекты игрока при дисконнекте.
        private void SpawnPlayer(ulong clientId)
        {
            var config = GameConfig.Instance;

            var (position, rotation) = NextSpawnPose();
            var pawn = Object.Instantiate(config.GhostPawnPrefab, position, rotation);
            pawn.SpawnWithOwnership(clientId);

            var controller = Object.Instantiate(config.PlayerControllerPrefab);
            controller.SpawnAsPlayerObject(clientId);
            controller.GetComponent<PlayerNetwork>().AssignPawn(pawn);
        }

        // Следующая точка спавна из загруженного мира (по кругу). Нет ни одной — спавним в начале
        // координат (мир без SpawnPoint — валидный кейс, напр. пустой sandbox).
        private (Vector3 position, Quaternion rotation) NextSpawnPose()
        {
            var points = new List<SpawnPoint>(Service.Get<WorldManager>().FindComponents<SpawnPoint>());
            if (points.Count == 0)
                return (Vector3.zero, Quaternion.identity);

            var point = points[m_NextSpawn++ % points.Count].transform;
            return (point.position, point.rotation);
        }

        public void Dispose()
        {
            GameState.Spawned -= OnGameStateSpawned;

            var ngo = NetworkManager.Singleton;
            if (ngo != null)
                ngo.OnClientConnectedCallback -= SpawnPlayer;
        }
    }
}
