using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // На пешке-герое (NetworkObject + owner-auth NetworkTransform + Entity + ActionMovement).
    // У НЕ-владельца отключает локальный исполнитель движения — там позицию гонит NetworkTransform
    // (реплика от владельца), и ActionMovement не должен с ней драться. Пешка ничего не создаёт и
    // никого не possess'ит — только настраивает свою же авторитетность движения.
    [RequireComponent(typeof(Entity))]
    public class PawnNetwork : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
                return;

            if (TryGetComponent<ActionMovement>(out var movement))
                movement.enabled = false;
        }
    }
}
