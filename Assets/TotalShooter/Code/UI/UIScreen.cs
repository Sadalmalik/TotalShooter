using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Базовый экран UI: показ/скрытие через SetActive. Навигацию между экранами ведёт UIManager
    // (ставит ссылку Manager), сами экраны дёргают сервисы через Service.Get<T>().
    public abstract class UIScreen : MonoBehaviour
    {
        public UIManager Manager { get; set; }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
