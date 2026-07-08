using NLua;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Контроллер, исполняющий пользовательский Lua-скрипт (AI/зональная логика). Скрипт занимает
    // то же место, что C#-логика контроллера: решает "что делать" и командует пешкой. Исполняется
    // только на хосте, результат реплицируется командным каналом (см. architecture-network.md).
    public class ScriptController : Controller
    {
        // NLua открывает всю stdlib + свои CLR-мостики по умолчанию — white-list значит, что их
        // обнуляем, а не что их безопасно оставить (миры — недоверенный расшариваемый контент).
        private static readonly string[] BlockedGlobals =
        {
            "os", "io", "debug", "dofile", "loadfile", "load", "require",
            "import", "load_assembly", "luanet", "make_object", "get_method_bysig",
        };

        private Lua m_Lua;

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
