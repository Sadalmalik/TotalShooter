using Sadalmalik.TotalShooter.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    public class UIJoinGame : UIScreen
    {
        [SerializeField] private TMP_InputField m_CodeField;
        [SerializeField] private TMP_InputField m_PasswordField;
        [SerializeField] private Button m_JoinButton;
        [SerializeField] private Button m_BackButton;
        [SerializeField] private TMP_Text m_StatusText;

        private void Awake()
        {
            m_JoinButton.onClick.AddListener(OnJoin);
            m_BackButton.onClick.AddListener(() => Manager.Open(Manager.MainMenu));
        }

        private async void OnJoin()
        {
            var code = m_CodeField.text;
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("Введите код комнаты");
                return;
            }

            m_JoinButton.interactable = false;
            SetStatus("Подключение...");
            try
            {
                // Клиент только присоединяется; игрока для него спавнит хост (GameManager) в
                // колбэке подключения NGO. Мир клиент грузит сам, узнав имя из GameState["world"].
                await Service.Get<SessionManager>().JoinSessionByCodeAsync(code, m_PasswordField.text);
                Service.Get<GameManager>().StartClient();
                Manager.Hud.SetJoinCode(code);
                Manager.Open(Manager.Hud);
            }
            catch (System.Exception e)
            {
                SetStatus($"Ошибка: {e.Message}");
            }
            finally
            {
                m_JoinButton.interactable = true;
            }
        }

        private void SetStatus(string text)
        {
            if (m_StatusText != null)
                m_StatusText.SetText(text);
        }
    }
}
