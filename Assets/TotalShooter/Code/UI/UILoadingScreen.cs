using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sadalmalik.TotalShooter
{
    public class UILoadingScreen : MonoBehaviour
    {
        private Image m_Fill;
        private Text m_Text;
        
        public IProgress<float> Progress;
        
        private void Awake()
        {
            Progress = new Progress<float>(UpdateProgressBar);
        }

        private void UpdateProgressBar(float progressValue)
        {
            m_Fill.fillAmount = progressValue;
        }
    }
}