using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Исполнитель движения врага — парный к ActionMovement (пешка игрока). Двигает CharacterController
    // к заданной точке: рулёж, гравитация, поворот по движению. Само "куда идти" решает мозг
    // (следование по flow-field / командир), здесь только исполнение намерения MoveTo(point).
    //
    // Сейчас — Update на каждом агенте (проще для рабочего слайса). Массовое движение сотен-тысяч
    // врагов — кандидат на централизацию (единый менеджер) / Unity.Jobs+Burst позже, по факту
    // узкого места (см. CLAUDE.md "Массовые юниты"). Не оптимизируем преждевременно.
    [RequireComponent(typeof(CharacterController))]
    public class NavAgent : MonoBehaviour
    {
        [SerializeField] private float m_Speed = 3.5f;
        [SerializeField] private float m_StoppingDistance = 0.3f;
        [SerializeField] private bool m_UseGravity = true;
        [SerializeField] private float m_Gravity = -9.81f;

        private CharacterController m_Controller;
        private float m_VerticalSpeed;
        private Vector3? m_Destination;

        public bool HasDestination => m_Destination.HasValue;

        // Достигнута ли текущая цель (по горизонтали, в пределах StoppingDistance). Мозг опрашивает,
        // чтобы выдать следующую точку пути.
        public bool Arrived => m_Destination is { } point && HorizontalDistance(point) <= m_StoppingDistance;

        private void Awake()
        {
            m_Controller = GetComponent<CharacterController>();
        }

        // Намерение: идти к мировой точке (движение — по горизонтали; вертикаль отдаёт гравитация).
        // Мозг обновляет цель по мере продвижения (следующая нода пути → финальная точка).
        public void MoveTo(Vector3 worldPoint)
        {
            m_Destination = worldPoint;
        }

        public void Stop()
        {
            m_Destination = null;
        }

        private void Update()
        {
            var velocity = Vector3.zero;

            if (m_Destination is { } point)
            {
                var to = point - transform.position;
                to.y = 0;
                if (to.magnitude > m_StoppingDistance)
                {
                    var direction = to.normalized;
                    velocity = direction * m_Speed;
                    Face(direction);
                }
            }

            if (m_UseGravity)
            {
                if (m_Controller.isGrounded && m_VerticalSpeed < 0)
                    m_VerticalSpeed = -2f; // лёгкий прижим к земле, чтобы isGrounded не мигал

                m_VerticalSpeed += m_Gravity * Time.deltaTime;
                velocity.y = m_VerticalSpeed;
            }

            m_Controller.Move(velocity * Time.deltaTime);
        }

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        private float HorizontalDistance(Vector3 point)
        {
            var to = point - transform.position;
            to.y = 0;
            return to.magnitude;
        }
    }
}
