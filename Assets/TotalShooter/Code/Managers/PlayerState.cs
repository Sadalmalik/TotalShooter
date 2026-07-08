using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;

namespace Sadalmalik.TotalShooter
{
    // Реплицируемое состояние игрока (NetworkObject, по одному на игрока). Две KV-секции с разным
    // правом записи (в NGO нет прав «по ключу»):
    //   Owner — пишет владелец (имя, аватар, скин);
    //   Host  — пишет хост (счёт, флаги, ачивки).
    // Читают обе — все. PvP-критичное кладём только в Host. Обе доступны из Lua.
    public class PlayerState : NetworkBehaviour
    {
        private readonly Blackboard m_Owner = new(writePerm: NetworkVariableWritePermission.Owner);
        private readonly Blackboard m_Host = new(); // write = Server по умолчанию

        // Запись — явно в нужную секцию: playerState.Owner["name"], playerState.Host["score"].
        public Blackboard Owner => m_Owner;
        public Blackboard Host => m_Host;

        // Удобное чтение по ключу без указания секции: сперва Owner, затем Host. Только чтение —
        // на запись секцию выбираем явно (иначе неоднозначно: хост своего же игрока — и Owner,
        // и Server одновременно).
        public object this[string key] => m_Owner.TryGet(key, out var value) ? value : m_Host[key];
    }
}
