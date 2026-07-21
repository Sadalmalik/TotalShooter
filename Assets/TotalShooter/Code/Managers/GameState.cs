using System;
using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;

namespace Sadalmalik.TotalShooter
{
    // Реплицируемое общее состояние матча как KV-blackboard: пишет хост (матч-Lua через
    // GameManager), читают все. Фазы/отсчёт/готовность и любую кастомную мету задаёт скрипт, не
    // хардкод C# — ради этого и KV вместо фиксированных полей. NetworkObject-мета-объект сессии.
    public class GameState : NetworkBehaviour
    {
        // Единственный GameState сессии находится клиентом статически (POCO-сервисы вроде
        // GameManager не могут держать сцен-ссылку заранее). Spawned — чтобы подписаться до того,
        // как объект реплицируется; Changed — чтобы дождаться прихода нужного ключа (напр. "world").
        public static event Action<GameState> Spawned;
        public static GameState Current { get; private set; }

        public event Action Changed;

        // Blackboard сам является NetworkList → NGO находит это поле и реплицирует. Дефолтное
        // право записи — Server (пишет хост), что нам и нужно.
        private readonly Blackboard m_Board = new();

        // Индексатор для Lua/C#: gameState["phase"] = "countdown".
        public object this[string key]
        {
            get => m_Board[key];
            set => m_Board[key] = value;
        }

        public override void OnNetworkSpawn()
        {
            Current = this;
            m_Board.OnListChanged += OnBoardChanged;
            Spawned?.Invoke(this);
        }

        public override void OnNetworkDespawn()
        {
            m_Board.OnListChanged -= OnBoardChanged;
            if (Current == this)
                Current = null;
        }

        private void OnBoardChanged(NetworkListEvent<KvEntry> _)
        {
            Changed?.Invoke();
        }
    }
}
