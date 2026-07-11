using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    public class UIMainMenu : UIScreen
    {
        [SerializeField] private Button m_CreateButton;
        [SerializeField] private Button m_JoinButton;
        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private Button m_QuitButton;

        private void Awake()
        {
            // Manager ставится UIManager'ом в его Awake; лямбды читают его при клике (позже), не сейчас.
            m_CreateButton.onClick.AddListener(() => Manager.Open(Manager.CreateGame));
            m_JoinButton.onClick.AddListener(() => Manager.Open(Manager.JoinGame));
            m_SettingsButton.onClick.AddListener(() => Manager.Open(Manager.Settings));
            m_QuitButton.onClick.AddListener(Application.Quit);
        }
    }
}
