using Sadalmalik.TotalShooter.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    public class UIMainMenu : UIScreen
    {
        [SerializeField] private TMP_InputField m_NameField;
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

            // Имя = auth-профиль игрока (см. SessionManager). Пишем при изменении поля, до создания/
            // входа в сессию. К моменту ввода SessionManager уже зарегистрирован GameStarter'ом.
            if (m_NameField != null)
                m_NameField.onValueChanged.AddListener(name => Service.Get<SessionManager>().PlayerName = name);
        }
    }
}
