using DG.Tweening;
using UnityEngine;

namespace Sadalmalik.TotalShooter.Models.Ghost
{
    public class GhostFloat : MonoBehaviour
    {
        [SerializeField]
        private float floatDistance = 0.5f; // Distance to move up

        [SerializeField]
        private float duration = 1.5f; // Time taken for one direction

        private void Start()
        {
            // Smoothly move up and down continuously
            transform.DOLocalMoveY(transform.localPosition.y + floatDistance, duration)
                .SetEase(Ease.InOutSine)      // Smooth slowdown at top and bottom
                .SetLoops(-1, LoopType.Yoyo); // -1 means infinite loops, Yoyo means back-and-forth
        }

        private void OnDestroy()
        {
            // Good practice to prevent memory leaks when changing scenes or destroying the object
            transform.DOKill();
        }
    }
}