using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Черновой bootstrap для локального теста в Play Mode: поднимает окружение, пешку-призрака,
    // камеру и контроллер, связывает их. Полноценный жизненный цикл сессии (WorldManager/
    // GameManager/сеть) — отдельная задача, см. code.md "Жизненный цикл сессии".
    public class GameStarter : MonoBehaviour
    {
        [SerializeField] private GameObject m_EnvironmentPrefab;
        [SerializeField] private Entity m_GhostPrefab;
        [SerializeField] private CameraOperator m_CameraOperatorPrefab;
        [SerializeField] private PlayerController m_PlayerControllerPrefab;

        private void Start()
        {
            if (m_EnvironmentPrefab != null)
                Instantiate(m_EnvironmentPrefab);

            // Оператор камеры создаётся вместе с контроллером; пешка — отдельно (призрак),
            // контроллер её possess'ит.
            var pawn = Instantiate(m_GhostPrefab);
            var camera = Instantiate(m_CameraOperatorPrefab);
            var controller = Instantiate(m_PlayerControllerPrefab);

            controller.Setup(camera, pawn);
        }
    }
}
