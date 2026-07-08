using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Исполнитель движения пешки: двигает Unity CharacterController по намерению, которое задаёт
    // контроллер (Move). Сам ввод не читает и про камеру не знает — контроллер уже разворачивает
    // направление относительно камеры. Врагам позже — свой исполнитель (NavGridAgent).
    [RequireComponent(typeof(CharacterController))]
    public class ActionMovement : MonoBehaviour
    {
        [SerializeField] private float m_Speed = 6f;
        [SerializeField] private bool m_UseGravity = true;
        [SerializeField] private float m_Gravity = -9.81f;

        private CharacterController m_Controller;
        private Vector3 m_MoveInput;
        private float m_VerticalSpeed;
        private Vector3? m_LookPoint;

        private void Awake()
        {
            m_Controller = GetComponent<CharacterController>();
        }

        // Намерение движения в мировых координатах: (x, z) — направление, y игнорируется.
        // Магнитуда клэмпится в 0..1 (диагональ не быстрее прямой).
        public void Move(Vector3 worldDirection)
        {
            worldDirection.y = 0;
            m_MoveInput = Vector3.ClampMagnitude(worldDirection, 1f);
        }

        // Намерение поворота: мировая точка, куда пешка должна смотреть (прицел). Контроллер
        // считает её рейкастом из камеры по мыши; здесь только исполняем разворот (по Y).
        public void LookAt(Vector3 worldPoint)
        {
            m_LookPoint = worldPoint;
        }

        private void Update()
        {
            var velocity = m_MoveInput * m_Speed;

            if (m_UseGravity)
            {
                if (m_Controller.isGrounded && m_VerticalSpeed < 0)
                    m_VerticalSpeed = -2f; // лёгкий прижим к земле, чтобы isGrounded не мигал

                m_VerticalSpeed += m_Gravity * Time.deltaTime;
                velocity.y = m_VerticalSpeed;
            }

            m_Controller.Move(velocity * Time.deltaTime);

            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (m_LookPoint is not { } point)
                return;

            var to = point - transform.position;
            to.y = 0;
            if (to.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(to);
        }
    }
}
