using UnityEngine;
using UnityEngine.InputSystem;

namespace Sadalmalik.TotalShooter
{
    public class CameraOperator : MonoBehaviour
    {
        [SerializeField] private Transform m_VerAxis;
        [SerializeField] private Transform m_HorAxis;
        [SerializeField] private Transform m_DistAxis;

        [Space]
        [SerializeField] private InputActionReference m_LookAction;
        [SerializeField] private InputActionReference m_ZoomAction;

        [Space]
        [SerializeField] private float m_MinZoom = 5;
        [SerializeField] private float m_MaxZoom = 25;
        [SerializeField] private float m_LookSensitivity = 0.1f;
        [SerializeField] private float m_ZoomSensitivity = 0.05f;
        
        private float m_AngleY;
        private float m_AngleX;
        private float m_Zoom;
        
        [Space]
        public Transform Target;

        private void Awake()
        {
            m_Zoom = m_MinZoom;
        }

        private void OnEnable()
        {
            m_LookAction.action.Enable();
            m_ZoomAction.action.Enable();
        }

        private void OnDisable()
        {
            m_LookAction.action.Disable();
            m_ZoomAction.action.Disable();
        }

        private void Update()
        {
            if (Target != null)
                transform.position = Target.position;
            
            var look = m_LookAction.action.ReadValue<Vector2>();
            var zoom = m_ZoomAction.action.ReadValue<float>();

            m_AngleY += look.x * m_LookSensitivity;
            m_AngleX = Mathf.Clamp(m_AngleX - look.y * m_LookSensitivity, -90, 90);
            m_Zoom = Mathf.Clamp(m_Zoom - zoom * m_ZoomSensitivity, m_MinZoom, m_MaxZoom);

            m_VerAxis.localRotation = Quaternion.Euler(0, m_AngleY, 0);
            m_HorAxis.localRotation = Quaternion.Euler(m_AngleX, 0, 0);
            m_DistAxis.localPosition = -m_Zoom * Vector3.forward;
        }
    }
}