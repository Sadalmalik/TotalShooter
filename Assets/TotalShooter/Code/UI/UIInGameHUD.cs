using Sadalmalik.TotalShooter.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    // Базовый каркас внутриигрового HUD: показывает join-код (для шаринга) и кнопку выхода.
    // Наполнение (HP/патроны/миникарта) — позже.
    public class UIInGameHUD : UIScreen
    {
        [SerializeField] private TMP_Text m_JoinCodeText;
        [SerializeField] private Button m_LeaveButton;

        private void Awake()
        {
            m_LeaveButton.onClick.AddListener(OnLeave);
        }

        public void SetJoinCode(string code)
        {
            if (m_JoinCodeText != null)
                m_JoinCodeText.SetText($"Code: {code}");
        }

        private async void OnLeave()
        {
            m_LeaveButton.interactable = false;
            try
            {
                await Service.Get<SessionManager>().LeaveSessionAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Leave failed: {e.Message}");
            }
            finally
            {
                m_LeaveButton.interactable = true;
                Manager.Open(Manager.MainMenu);
            }
        }
    }
}
