using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class CameraOperator : MonoBehaviour
    {
        [SerializeField]
        private Transform m_VerAxis;
        [SerializeField]
        private Transform m_HorAxis;
        [SerializeField]
        private Transform m_DistAxis;

        [Space]
        [SerializeField]
        private float m_MinZoom = 5;
        [SerializeField]
        private float m_MaxZoom = 25;
        
        private float m_AngleY;
        private float m_AngleX;
        private float m_Zoom;


        private void Update()
        {
            var y = Input.GetAxis("Mouse X");
            var x = Input.GetAxis("Mouse Y");
            var z = Input.GetAxis("Mouse ScrollWheel");

            m_AngleY += y;
            m_AngleX = Mathf.Clamp(m_AngleX + x, -90, 90);
            m_Zoom = Mathf.Clamp(m_Zoom + z, m_MinZoom, m_MaxZoom);

            m_VerAxis.localRotation = Quaternion.Euler(0, m_AngleY, 0);
            m_HorAxis.localRotation = Quaternion.Euler(m_AngleX, 0, 0);
            m_DistAxis.localPosition = -m_Zoom * Vector3.forward;
        }
    }
}