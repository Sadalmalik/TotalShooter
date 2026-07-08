using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Оператор камеры: следует за текущей пешкой (Target) и вращается вокруг неё. Ввод не читает —
    // контроллер вызывает Look/Zoom (Look — только в активном режиме камеры). Активный/пассивный
    // режим и курсор — на стороне контроллера; оператор просто исполняет.
    public class CameraOperator : MonoBehaviour
    {
        [SerializeField] private Transform m_VerAxis;   // рыскание (yaw, вокруг Y)
        [SerializeField] private Transform m_HorAxis;   // тангаж (pitch, вокруг X)
        [SerializeField] private Transform m_DistAxis;  // дистанция (zoom)
        [SerializeField] private Camera m_Camera;       // сама камера в риге (для рейкаста прицела)

        [Space]
        [SerializeField] private float m_MinZoom = 5f;
        [SerializeField] private float m_MaxZoom = 25f;
        [SerializeField] private float m_LookSensitivity = 0.1f;
        [SerializeField] private float m_ZoomSensitivity = 0.05f;

        private float m_AngleY;
        private float m_AngleX;
        private float m_Zoom;

        // Пешка, за позицией которой следует камера. Ставит контроллер при possession.
        public Transform Target { get; set; }

        // Горизонтальная ориентация камеры (только yaw) — контроллер разворачивает ей WASD в
        // мировое направление для camera-relative движения.
        public Quaternion PlanarRotation => Quaternion.Euler(0, m_AngleY, 0);

        // Сама камера в риге — контроллер строит из неё рейкаст прицела по позиции мыши.
        public Camera Camera => m_Camera;

        private void Awake()
        {
            m_Zoom = m_MinZoom;
            ApplyRotation();
            ApplyZoom();
        }

        // Поворот камеры на дельту движения мыши. Контроллер зовёт только в активном режиме.
        public void Look(Vector2 delta)
        {
            m_AngleY += delta.x * m_LookSensitivity;
            m_AngleX = Mathf.Clamp(m_AngleX - delta.y * m_LookSensitivity, -90f, 90f);
            ApplyRotation();
        }

        public void Zoom(float delta)
        {
            m_Zoom = Mathf.Clamp(m_Zoom - delta * m_ZoomSensitivity, m_MinZoom, m_MaxZoom);
            ApplyZoom();
        }

        // Позицию тянем за пешкой в LateUpdate — после того как ActionMovement подвинул её в
        // Update, иначе камера дёргается на кадр позади.
        private void LateUpdate()
        {
            if (Target != null)
                transform.position = Target.position;
        }

        private void ApplyRotation()
        {
            m_VerAxis.localRotation = Quaternion.Euler(0, m_AngleY, 0);
            m_HorAxis.localRotation = Quaternion.Euler(m_AngleX, 0, 0);
        }

        private void ApplyZoom()
        {
            m_DistAxis.localPosition = -m_Zoom * Vector3.forward;
        }
    }
}
