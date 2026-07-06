using NLua;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class Controller : MonoBehaviour
    {
        // NLua opens the full stdlib + its own CLR-bridge globals by default —
        // white-list means these get nulled out, not that they were safe to leave.
        private static readonly string[] BlockedGlobals =
        {
            "os", "io", "debug", "dofile", "loadfile", "load", "require",
            "import", "load_assembly", "luanet", "make_object", "get_method_bysig",
        };

        public Entity Entity { get; private set; }

        private Lua m_Lua;

        public void Possess(Entity entity, bool force = false)
        {
            if (Entity != null)
                Entity.controller = null;
            if (entity != null && (force || entity.controller == null))
                Entity = entity;
            if (Entity != null)
                Entity.controller = this;
        }

        public void LoadScript(string source, string chunkName)
        {
            m_Lua?.Dispose();
            m_Lua = new Lua();

            foreach (var name in BlockedGlobals)
                m_Lua[name] = null;

            m_Lua["entity"] = Entity;
            m_Lua.DoString(source, chunkName);
        }

        private void Update()
        {
            m_Lua?.GetFunction("OnUpdate")?.Call(Time.deltaTime);
        }

        private void OnDestroy()
        {
            m_Lua?.Dispose();
        }
    }
}