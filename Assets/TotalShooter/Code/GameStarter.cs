using Sadalmalik.TotalShooter.Architecture;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sadalmalik.TotalShooter
{
    // Bootstrap стартовой сцены: создаёт рантайм-сцену под контент, регистрирует менеджеры-сервисы
    // и запускает UI-меню. Игровые префабы лежат в GameConfig (Resources), поэтому здесь только
    // ссылка на UI. Движение проверяется через create-game одним инстансом (хост сам себе клиент).
    public class GameStarter : MonoBehaviour
    {
        // Имя рантайм-сцены под весь инстанцированный контент (мир + сетевые объекты).
        private const string RuntimeSceneName = "Game";

        [SerializeField] private UIManager m_UI;

        private void Start()
        {
            // Пустая активная сцена под весь рантайм-контент: инстанцированное падает в неё, а не в
            // бутстрап-сцену. Плоско в корне (без общего Transform-родителя — тысячи детей тормозят
            // Unity). Создаётся локально на каждом клиенте до сети; NGO scene management выключен.
            var runtimeScene = SceneManager.CreateScene(RuntimeSceneName);
            SceneManager.SetActiveScene(runtimeScene);

            // Менеджеры — POCO-сервисы, достаются везде через Service.Get<T>().
            Service.Add(new WorldManager());
            Service.Add(new SessionManager());
            Service.Add(new GameManager());

            if (m_UI != null)
                m_UI.ShowMainMenu();
        }

        // Чистим реестр сервисов на выходе — статик переживает выход из Play Mode (domain reload
        // может быть отключён), иначе в следующий заход потащим мёртвые ссылки.
        private void OnApplicationQuit()
        {
            Service.RemoveAll();
        }
    }
}
