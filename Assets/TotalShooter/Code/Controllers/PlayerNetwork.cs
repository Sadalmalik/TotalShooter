using System.Collections;
using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Сетевой адаптер объекта контроллера игрока. Композиция вместо наследования: сам
    // PlayerController — обычный Controller, а сеть (владение/видимость/назначение пешки) — здесь.
    //   1) видимость только владельцу (+ хост всегда);
    //   2) хост через AssignPawn отдаёт контроллеру пешку (какую заспавнил GameManager);
    //   3) у владельца поднимает камеру и делает Possess назначенной пешки, включает контроллер.
    // Пешку создаёт GameManager, а не контроллер/пешка — контроллер только берёт назначенную.
    [RequireComponent(typeof(PlayerController))]
    public class PlayerNetwork : NetworkBehaviour
    {
        // Какую пешку possess'ить. Пишет сервер (GameManager), читают все; владелец реагирует.
        private readonly NetworkVariable<NetworkObjectReference> m_Pawn = new();

        private PlayerController m_Controller;

        private void Awake()
        {
            m_Controller = GetComponent<PlayerController>();
            m_Controller.enabled = false; // включит владелец, когда получит пешку
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                NetworkObject.CheckObjectVisibility = clientId => clientId == OwnerClientId;

            if (IsOwner)
                StartCoroutine(OwnerSetup());
        }

        // Вызывает хост (GameManager) после спавна: раздаёт контроллеру его пешку.
        public void AssignPawn(NetworkObject pawn)
        {
            m_Pawn.Value = pawn;
        }

        private IEnumerator OwnerSetup()
        {
            // Камера — локальная (владелец создаёт свою; это его "глаза", не пешка).
            var camera = Instantiate(GameConfig.Instance.CameraOperatorPrefab);

            // Ждём, пока назначенная пешка появится и зарезолвится на нашем клиенте (порядок
            // прихода спавнов и NetworkVariable не гарантирован).
            NetworkObject pawnObject;
            while (!m_Pawn.Value.TryGet(out pawnObject))
                yield return null;

            m_Controller.enabled = true;
            m_Controller.Setup(camera, pawnObject.GetComponent<Entity>());
        }
    }
}
