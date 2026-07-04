using UnityEngine;

namespace Sadalmalik.TotalShooter
{
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