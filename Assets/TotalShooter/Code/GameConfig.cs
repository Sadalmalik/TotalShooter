using Sadalmalik.TotalShooter.Architecture;
using Unity.Netcode;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Глобальный конфиг игры: префабы сессии/пешек/камеры в одном месте. Ассет —
    // Resources/GameConfig.asset (меню Create → TotalShooter → Game Config). Достаётся везде
    // через GameConfig.Instance. Окружение больше не префаб — его роль играет загружаемый мир.
    [CreateAssetMenu(menuName = "TotalShooter/Game Config", fileName = "GameConfig")]
    public class GameConfig : SingletonScriptableObject<GameConfig>
    {
        public GameState GameStatePrefab;
        public NetworkObject PlayerControllerPrefab; // NetworkObject: PlayerController + PlayerNetwork
        public NetworkObject GhostPawnPrefab;         // NetworkObject: Entity + ActionMovement + NetworkTransform + PawnNetwork
        public CameraOperator CameraOperatorPrefab;   // локальный, не сетевой
    }
}
