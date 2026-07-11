using Sadalmalik.TotalShooter.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    public class UICreateGame : UIScreen
    {
        [SerializeField] private TMP_InputField m_NameField;
        [SerializeField] private TMP_InputField m_PasswordField;
        [SerializeField] private int m_MaxPlayers = 8;
        [SerializeField] private Button m_CreateButton;
        [SerializeField] private Button m_BackButton;
        [SerializeField] private TMP_Text m_StatusText;

        private void Awake()
        {
            m_CreateButton.onClick.AddListener(OnCreate);
            m_BackButton.onClick.AddListener(() => Manager.Open(Manager.MainMenu));
        }

        private async void OnCreate()
        {
            m_CreateButton.interactable = false;
            SetStatus("Создание сессии...");
            try
            {
                var name = string.IsNullOrWhiteSpace(m_NameField.text) ? "Game" : m_NameField.text;
                var code = await Service.Get<SessionManager>()
                    .CreateSessionAsync(name, m_MaxPlayers, m_PasswordField.text);

                // Хост поднят Sessions API'ем → спавним GameState + игроков.
                Service.Get<GameManager>().StartHost();

                Manager.Hud.SetJoinCode(code);
                Manager.Open(Manager.Hud);
            }
            catch (System.Exception e)
            {
                SetStatus($"Ошибка: {e.Message}");
            }
            finally
            {
                m_CreateButton.interactable = true;
            }
        }

        private void SetStatus(string text)
        {
            if (m_StatusText != null)
                m_StatusText.SetText(text);
        }
    }
}
