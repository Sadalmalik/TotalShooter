using UnityEngine;
using UnityEngine.InputSystem;

namespace Sadalmalik.TotalShooter
{
    // Локальный контроллер игрока: читает ввод и раздаёт его пешке (движение) и оператору камеры
    // (мышь/зум), управляет possession призрак↔персонаж. Сетевую часть (NetworkObject, скрытие от
    // чужих клиентов) навесим в группе «Сеть».
    public class PlayerController : Controller
    {
        [SerializeField] private CameraOperator m_Camera;

        // Пешка, которой владеем на старте (для локального теста). В бою possession делает
        // GameManager: сначала призрак, затем персонаж.
        [SerializeField] private Entity m_InitialPawn;

        [Space]
        [SerializeField] private InputActionReference m_MoveAction;
        [SerializeField] private InputActionReference m_LookAction;
        [SerializeField] private InputActionReference m_ZoomAction;
        [SerializeField] private InputActionReference m_RotateCameraAction; // hold → активный режим

        private bool m_CameraActive;

        private void Start()
        {
            if (m_InitialPawn != null)
                Possess(m_InitialPawn);
        }

        // Внешняя связка (GameStarter/GameManager инстанцирует контроллер, камеру и пешку отдельно
        // и сводит их здесь). Для standalone-теста можно вместо этого назначить поля в инспекторе.
        public void Setup(CameraOperator camera, Entity pawn)
        {
            m_Camera = camera;
            Possess(pawn);
        }

        private void OnEnable()
        {
            m_MoveAction?.action.Enable();
            m_LookAction?.action.Enable();
            m_ZoomAction?.action.Enable();
            m_RotateCameraAction?.action.Enable();
        }

        private void OnDisable()
        {
            m_MoveAction?.action.Disable();
            m_LookAction?.action.Disable();
            m_ZoomAction?.action.Disable();
            m_RotateCameraAction?.action.Disable();
            SetCameraActive(false);
        }

        private void Update()
        {
            UpdateCamera();
            UpdatePawn();
        }

        private void UpdateCamera()
        {
            if (m_Camera == null)
                return;

            m_Camera.Target = Entity != null ? Entity.transform : null;

            // Активный режим камеры — пока зажата спец-кнопка: курсор скрыт, мышь крутит камеру.
            var active = m_RotateCameraAction != null && m_RotateCameraAction.action.IsPressed();
            SetCameraActive(active);

            if (active && m_LookAction != null)
                m_Camera.Look(m_LookAction.action.ReadValue<Vector2>());

            if (m_ZoomAction != null)
                m_Camera.Zoom(m_ZoomAction.action.ReadValue<float>());
        }

        private void UpdatePawn()
        {
            if (Entity == null || !Entity.TryGetComponent<ActionMovement>(out var movement))
                return;

            // Движение: WASD, camera-relative (разворот относительно yaw камеры).
            if (m_MoveAction != null)
            {
                var input = m_MoveAction.action.ReadValue<Vector2>();
                var direction = new Vector3(input.x, 0, input.y);
                if (m_Camera != null)
                    direction = m_Camera.PlanarRotation * direction;
                movement.Move(direction);
            }

            Aim(movement);
        }

        // Прицеливание: рейкаст из камеры по позиции мыши на горизонтальную плоскость пешки →
        // мировая точка, куда пешке смотреть.
        private void Aim(ActionMovement movement)
        {
            if (m_Camera == null || m_Camera.Camera == null || Mouse.current == null)
                return;

            var ray = m_Camera.Camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.up, Entity.transform.position);
            if (plane.Raycast(ray, out var enter))
                movement.LookAt(ray.GetPoint(enter));
        }

        private void SetCameraActive(bool active)
        {
            if (active == m_CameraActive)
                return;

            m_CameraActive = active;
            Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !active;
        }
    }
}
