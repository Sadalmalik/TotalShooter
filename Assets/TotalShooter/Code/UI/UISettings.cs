using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    // Заглушка настроек — пока только выход назад. Наполнение (звук/управление/графика) — позже.
    public class UISettings : UIScreen
    {
        [SerializeField] private Button m_BackButton;

        private void Awake()
        {
            m_BackButton.onClick.AddListener(() => Manager.Open(Manager.MainMenu));
        }
    }
}
