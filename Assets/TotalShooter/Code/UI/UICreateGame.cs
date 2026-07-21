using System.Collections.Generic;
using Sadalmalik.TotalShooter.Architecture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    public class UICreateGame : UIScreen
    {
        [SerializeField] private TMP_InputField m_NameField;
        [SerializeField] private TMP_Dropdown m_WorldDropdown;
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

        // Экран показывается/прячется через SetActive → OnEnable = момент открытия. Перечитываем
        // список миров каждый раз (папку могли пополнить, не перезапуская игру).
        private void OnEnable()
        {
            RefreshWorlds();
        }

        private void RefreshWorlds()
        {
            if (m_WorldDropdown == null)
                return;

            m_WorldDropdown.ClearOptions();
            m_WorldDropdown.AddOptions(new List<string>(WorldManager.ListWorlds()));
        }

        private async void OnCreate()
        {
            var world = SelectedWorld();
            if (world == null)
            {
                SetStatus("Нет доступных миров (папка Worlds/ рядом с билдом пуста).");
                return;
            }

            m_CreateButton.interactable = false;
            SetStatus("Создание сессии...");
            try
            {
                var name = string.IsNullOrWhiteSpace(m_NameField.text) ? "Game" : m_NameField.text;
                var code = await Service.Get<SessionManager>()
                    .CreateSessionAsync(name, m_MaxPlayers, m_PasswordField.text);

                // Хост поднят Sessions API'ем → грузим выбранный мир локально, спавним GameState.
                Service.Get<GameManager>().StartHost(world);

                Manager.Hud.SetJoinCode(code);
                Manager.Open(Manager.Hud);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                SetStatus(e.ToString());
            }
            finally
            {
                m_CreateButton.interactable = true;
            }
        }

        // Имя выбранного в дропдауне мира; null — если миров нет (дропдаун пуст).
        private string SelectedWorld()
        {
            if (m_WorldDropdown == null || m_WorldDropdown.options.Count == 0)
                return null;

            return m_WorldDropdown.options[m_WorldDropdown.value].text;
        }

        private void SetStatus(string text)
        {
            if (m_StatusText != null)
                m_StatusText.SetText(text);
        }
    }
}
