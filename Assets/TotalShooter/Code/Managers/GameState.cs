using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;

namespace Sadalmalik.TotalShooter
{
    // Реплицируемое общее состояние матча как KV-blackboard: пишет хост (матч-Lua через
    // GameManager), читают все. Фазы/отсчёт/готовность и любую кастомную мету задаёт скрипт, не
    // хардкод C# — ради этого и KV вместо фиксированных полей. NetworkObject-мета-объект сессии.
    public class GameState : NetworkBehaviour
    {
        // Default write permission NetworkList = Server → пишет только хост, что нам и нужно.
        private readonly NetworkList<KvEntry> m_State = new();

        private Blackboard m_Board;

        private void Awake()
        {
            m_Board = new Blackboard(m_State);
        }

        // Индексатор для Lua/C#: gameState["phase"] = "countdown".
        public object this[string key]
        {
            get => m_Board[key];
            set => m_Board[key] = value;
        }
    }
}
