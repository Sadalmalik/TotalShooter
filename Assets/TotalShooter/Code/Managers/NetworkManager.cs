using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace Sadalmalik.TotalShooter
{
    // Транспорт/сессия: поднимает Unity Services и создаёт/присоединяет игровую сессию через
    // Sessions API (Relay+Lobby под капотом; сам стартует NGO-хост/клиент). Спавн игроков и фазы
    // матча — не здесь, это GameManager. POCO-сервис (не MonoBehaviour): реактивный, Unity-цикл не
    // нужен. Наш NetworkManager != Unity.Netcode.NetworkManager (тем рулит Sessions API под капотом).
    public class NetworkManager
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
