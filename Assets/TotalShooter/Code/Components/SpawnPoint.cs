using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    // Маркер точки спавна игрока. Пустой компонент — значима только позиция/поворот его Transform.
    // Ставится на Entity (в редакторе мира → SceneConverter → world.json). GameManager (host-only)
    // собирает все SpawnPoint загруженного мира и раздаёт подключающимся игрокам по кругу.
    public class SpawnPoint : MonoBehaviour
    {
    }
}
