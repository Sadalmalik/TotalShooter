using System;
using System.Collections.Generic;
using Sadalmalik.TotalShooter.Architecture;
using UnityEngine;

namespace Sadalmalik.TotalShooter
{
    public enum NavShape
    {
        Circle,
        Box,
    }

    // Переход к соседней ноде: EntityId соседа + тип перехода. Тип — строка (а не enum) ради
    // данных-ориентированности/моддинга: мир/мод может ввести свой тип перехода без перекомпиляции.
    // Канонические типы — в NavConnectionType. Позже сюда же можно добавить теги (список строк).
    [Serializable]
    public struct NavConnection
    {
        public int Id;
        public string Type;
    }

    // Канонические типы переходов (поле Type — открытая строка, это лишь подсказки от опечаток).
    public static class NavConnectionType
    {
        public const string Walk = "walk";     // обычная "широкая дорога" по полу
        public const string Jump = "jump";     // прыжок (в т.ч. через уступ/платформу)
        public const string Ladder = "ladder"; // лестница/залезание
        public const string Vent = "vent";     // узкий проход (вентиляция и т.п.)
    }

    // Нода нав-графа — не точка, а ОБЛАСТЬ, живёт на своём Entity (сохраняется/редактируется как
    // любой Entity). Форма (круг/прямоугольник) + высота задаются в локальных координатах: позиция
    // и поворот Transform влияют на область и гизмо. Нода ЛЕЖИТ НА ПОЛУ: позиция — уровень пола,
    // объём идёт вверх на Height (удобно для генерации — класть ноды рейкастом на пол). Соседи —
    // NavConnection (id + тип перехода), сериализуются обычным ComponentOverrides; резолв id →
    // NavNode — через реестр мира.
    //
    // Правило графа ("широкая дорога"): из любой точки ноды можно дойти до любой точки соседней —
    // осознанное упрощение математики поиска пути, см. .claude/navigation_system.md. Масштаб
    // Transform игнорируется — размеры в мировых единицах.
    public class NavNode : MonoBehaviour
    {
        [SerializeField] private NavShape m_Shape = NavShape.Circle;
        [SerializeField] private float m_Radius = 3f; // круг
        [SerializeField] private float m_Width = 3f;  // прямоугольник, локальный X
        [SerializeField] private float m_Length = 3f; // прямоугольник, локальный Z
        [SerializeField] private float m_Height = 2f; // высота области (обе формы), вверх от пола
        [SerializeField] private List<NavConnection> m_Neighbours = new();

        public NavShape Shape => m_Shape;
        public float Radius => m_Radius;
        public float Width => m_Width;
        public float Length => m_Length;
        public float Height => m_Height;
        public IReadOnlyList<NavConnection> Neighbours => m_Neighbours;

        // Соседи как (NavNode, тип перехода): резолв EntityId → Entity → NavNode через реестр мира.
        // Рантайм (в редакторе реестра нет — там резолвят обходом сцены, см. NavNodeEditor).
        public IEnumerable<(NavNode node, string type)> ResolveNeighbours()
        {
            var world = Service.Get<WorldManager>();
            foreach (var connection in m_Neighbours)
            {
                var entity = world.Get(connection.Id);
                if (entity != null && entity.TryGetComponent<NavNode>(out var node))
                    yield return (node, connection.Type);
            }
        }

        // Лежит ли мировая точка внутри области. Позиция/поворот учтены (переводим в локальные
        // координаты вращением, не InverseTransformPoint — чтобы игнорировать масштаб). Высота —
        // от пола вверх: localY в [0, Height].
        public bool Contains(Vector3 worldPoint)
        {
            var local = Quaternion.Inverse(transform.rotation) * (worldPoint - transform.position);
            if (local.y < 0f || local.y > m_Height)
                return false;

            if (m_Shape == NavShape.Circle)
                return local.x * local.x + local.z * local.z <= m_Radius * m_Radius;

            return Mathf.Abs(local.x) <= m_Width * 0.5f && Mathf.Abs(local.z) <= m_Length * 0.5f;
        }

        // Ближайшая точка области к worldPoint (клампит внутрь формы и высоты). Для движения
        // "куда угодно в пределах ноды" (стринг-пуллинг) и визуализации соединений.
        public Vector3 ClosestPoint(Vector3 worldPoint)
        {
            var rotation = transform.rotation;
            var local = Quaternion.Inverse(rotation) * (worldPoint - transform.position);

            if (m_Shape == NavShape.Circle)
            {
                var radiusSqr = m_Radius * m_Radius;
                if (local.x * local.x + local.z * local.z > radiusSqr)
                {
                    var xz = new Vector2(local.x, local.z).normalized * m_Radius;
                    local.x = xz.x;
                    local.z = xz.y;
                }
            }
            else
            {
                local.x = Mathf.Clamp(local.x, -m_Width * 0.5f, m_Width * 0.5f);
                local.z = Mathf.Clamp(local.z, -m_Length * 0.5f, m_Length * 0.5f);
            }

            local.y = Mathf.Clamp(local.y, 0f, m_Height);
            return transform.position + rotation * local;
        }

        // Контур области в мировых координатах на высоте yOffset от пола (позиции ноды). segments —
        // детализация круга (у прямоугольника всегда 4 угла). Для гизмо и контакт-теста линковки.
        public void SampleOutline(List<Vector3> into, int segments, float yOffset)
        {
            into.Clear();
            var rotation = transform.rotation;
            var origin = transform.position;

            if (m_Shape == NavShape.Box)
            {
                var hx = m_Width * 0.5f;
                var hz = m_Length * 0.5f;
                into.Add(origin + rotation * new Vector3(-hx, yOffset, -hz));
                into.Add(origin + rotation * new Vector3(hx, yOffset, -hz));
                into.Add(origin + rotation * new Vector3(hx, yOffset, hz));
                into.Add(origin + rotation * new Vector3(-hx, yOffset, hz));
                return;
            }

            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float) segments * Mathf.PI * 2f;
                into.Add(origin + rotation * new Vector3(Mathf.Cos(angle) * m_Radius, yOffset, Mathf.Sin(angle) * m_Radius));
            }
        }
    }
}
