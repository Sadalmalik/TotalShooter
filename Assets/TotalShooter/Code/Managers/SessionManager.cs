using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace Sadalmalik.TotalShooter
{
    // "Входная дверь" в сетевую сессию (не путать с движком репликации Unity.Netcode.NetworkManager
    // — это разные слои). Поднимает Unity Services и создаёт/находит/присоединяет/покидает игровую
    // сессию через Sessions API (Relay-аллокация + Lobby-комната под капотом; сам стартует NGO
    // хост/клиент). Транспорт, спавн, синк — это уже NGO. Спавн игроков и фазы матча — GameManager.
    // POCO-сервис: реактивный, Unity-цикл не нужен.
    public class SessionManager
    {
        public ISession CurrentSession { get; private set; }
        public bool IsInSession => CurrentSession != null;

        private bool m_ServicesReady;

        // Инициализация Unity Services + анонимный вход — один раз за запуск.
        public async Task EnsureServicesAsync()
        {
            if (m_ServicesReady)
                return;

            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            m_ServicesReady = true;
        }

        // Хост: создаёт сессию с Relay-сетью, возвращает join-код для шаринга.
        public async Task<string> CreateSessionAsync(string sessionName, int maxPlayers, string password = null)
        {
            await EnsureServicesAsync();

            var options = new SessionOptions
            {
                Name = sessionName,
                MaxPlayers = maxPlayers,
                Password = string.IsNullOrEmpty(password) ? null : password,
            }.WithRelayNetwork();

            CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            return CurrentSession.Code;
        }

        // Клиент: присоединяется по join-коду.
        public async Task JoinSessionByCodeAsync(string code, string password = null)
        {
            await EnsureServicesAsync();

            var options = new JoinSessionOptions();
            if (!string.IsNullOrEmpty(password))
                options.Password = password;

            CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options);
        }

        public async Task LeaveSessionAsync()
        {
            if (CurrentSession == null)
                return;

            await CurrentSession.LeaveAsync();
            CurrentSession = null;
        }
    }
}
