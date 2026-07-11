using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Сетевой адаптер объекта управления игроком: даёт ему сетевую идентичность (NetworkObject),
    // владение и видимость, но саму логику не подменяет. Композиция вместо наследования — сам
    // PlayerController остаётся обычным Controller (possession/ввод/камера), а не NetworkBehaviour,
    // чтобы базовый Controller не тащил сеть (её не должно быть у AI-ScriptController).
    //
    // Делает две вещи:
    //   1) скрывает объект от чужих клиентов (только владелец + хост видят) — экономия трафика;
    //   2) включает PlayerController ТОЛЬКО у владельца. Критично: на хосте физически есть копии
    //      контроллеров всех игроков, и без гейта они все читали бы локальный ввод хоста.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerNetwork : NetworkBehaviour
    {
        private PlayerController m_Controller;

        private void Awake()
        {
            m_Controller = GetComponent<PlayerController>();
            m_Controller.enabled = false; // владелец включит в OnNetworkSpawn
        }

        public override void OnNetworkSpawn()
        {
            // Видимость только владельцу (хост-сервер видит всегда, предикат к нему не применяется).
            // Late-joiner'ы тоже проверяются этим предикатом.
            if (IsServer)
                NetworkObject.CheckObjectVisibility = clientId => clientId == OwnerClientId;

            m_Controller.enabled = IsOwner;
        }
    }
}
