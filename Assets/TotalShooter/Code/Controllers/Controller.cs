using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Базовый контроллер: владеет пешкой (possession). Общее для игрока (PlayerController) и
    // скриптового/AI-контроллера (ScriptController). Ничего кроме possession здесь не держим.
    public class Controller : MonoBehaviour
    {
        public Entity Entity { get; private set; }

        public void Possess(Entity entity, bool force = false)
        {
            if (Entity != null)
                Entity.controller = null;
            if (entity != null && (force || entity.controller == null))
                Entity = entity;
            if (Entity != null)
                Entity.controller = this;
        }
    }
}
