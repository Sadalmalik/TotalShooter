using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Навигатор экранов меню: держит все экраны, показывает по одному. Регистрацию сервисов делает
    // GameStarter (единый бутстрап); здесь — только UI. Экраны просят навигацию через Open().
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private UIMainMenu m_MainMenu;
        [SerializeField] private UICreateGame m_CreateGame;
        [SerializeField] private UIJoinGame m_JoinGame;
        [SerializeField] private UISettings m_Settings;
        [SerializeField] private UIInGameHUD m_Hud;

        public UIMainMenu MainMenu => m_MainMenu;
        public UICreateGame CreateGame => m_CreateGame;
        public UIJoinGame JoinGame => m_JoinGame;
        public UISettings Settings => m_Settings;
        public UIInGameHUD Hud => m_Hud;

        private UIScreen[] m_Screens;

        private void Awake()
        {
            m_Screens = new UIScreen[] { m_MainMenu, m_CreateGame, m_JoinGame, m_Settings, m_Hud };
            foreach (var screen in m_Screens)
                if (screen != null)
                    screen.Manager = this;
        }

        public void ShowMainMenu() => Open(m_MainMenu);

        // Показывает один экран, остальные прячет.
        public void Open(UIScreen target)
        {
            foreach (var screen in m_Screens)
                if (screen != null)
                    screen.gameObject.SetActive(screen == target);
        }
    }
}
