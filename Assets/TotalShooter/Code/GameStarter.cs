using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public class GameStarter : MonoBehaviour
    {
        // Pre-Instantiated
        private UILoadingScreen m_LoadingScreen;
        
        // Sample logic
        [SerializeField]
        private GameObject m_EnvironmentPrefab;
        [SerializeField]
        private Entity m_UnitPrefab;
        [SerializeField]
        private CameraOperator m_CameraOperatorPrefab;
        
        
        private void Start()
        {
            // Start game here
            
            Instantiate(m_EnvironmentPrefab);
            
            var unit = Instantiate(m_UnitPrefab);
            var cameraOperator = Instantiate(m_CameraOperatorPrefab);
            cameraOperator.Target = unit.transform;
        }
    }
}