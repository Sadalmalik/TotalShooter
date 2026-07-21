using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sadalmalik.TotalShooter.Editor
{
    // Инспектор + гизмо для NavNode: рисует объёмную форму области (пол/потолок + рёбра), линии
    // соединений и конвексную "ленту" прохода между связанными нодами (визуализация правила
    // "широкая дорога"). Кнопки линковки: по дистанции центров, по соприкосновению форм; ручная
    // правка — через список Neighbours в дефолтном инспекторе (id соседей).
    //
    // Соседи хранятся по EntityId. EntityId присваивается обычно на сохранении мира
    // (SceneConverter), но линковать надо в редакторе раньше — поэтому кнопки при необходимости
    // сами выдают стабильные id involved-нодам (совместимо с SceneConverter: он сохраняет ненулевые).
    [CustomEditor(typeof(NavNode))]
    [CanEditMultipleObjects]
    public class NavNodeEditor : UnityEditor.Editor
    {
        private static float s_LinkDistance = 6f;
        private static string s_ConnectionType = NavConnectionType.Walk;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Linking", EditorStyles.boldLabel);
            s_LinkDistance = EditorGUILayout.FloatField("Link Distance", s_LinkDistance);
            s_ConnectionType = EditorGUILayout.TextField("Connection Type", s_ConnectionType);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Link by Distance"))
                    ForEachTarget(node => LinkByDistance(node, s_LinkDistance, s_ConnectionType));

                if (GUILayout.Button("Link by Contact"))
                    ForEachTarget(node => LinkByContact(node, s_ConnectionType));
            }

            if (GUILayout.Button("Clear Links"))
                ForEachTarget(ClearLinks);
        }

        private void ForEachTarget(System.Action<NavNode> action)
        {
            foreach (var obj in targets)
                if (obj is NavNode node)
                    action(node);

            EditorSceneManager.MarkAllScenesDirty();
        }

#region Линковка

        private static void LinkByDistance(NavNode node, float distance, string type)
        {
            var context = new IdContext();
            foreach (var other in AllNodes())
            {
                if (other == node)
                    continue;
                if (Vector3.Distance(node.transform.position, other.transform.position) <= distance)
                    LinkPair(node, other, context, type);
            }
        }

        private static void LinkByContact(NavNode node, string type)
        {
            var context = new IdContext();
            foreach (var other in AllNodes())
            {
                if (other == node)
                    continue;
                if (ShapesContact(node, other))
                    LinkPair(node, other, context, type);
            }
        }

        private static void ClearLinks(NavNode node)
        {
            var so = new SerializedObject(node);
            so.FindProperty("m_Neighbours").ClearArray();
            so.ApplyModifiedProperties();
        }

        // Симметричная связь по EntityId (в обе стороны), с дедупликацией. Тип перехода — общий
        // для обоих направлений (пока переходы симметричны; для односторонних — отдельная задача).
        private static void LinkPair(NavNode a, NavNode b, IdContext context, string type)
        {
            var idA = context.Ensure(a);
            var idB = context.Ensure(b);
            AddNeighbour(a, idB, type);
            AddNeighbour(b, idA, type);
        }

        private static void AddNeighbour(NavNode node, int id, string type)
        {
            var so = new SerializedObject(node);
            var list = so.FindProperty("m_Neighbours");

            for (var i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("Id").intValue == id)
                    return; // уже есть

            list.arraySize++;
            var element = list.GetArrayElementAtIndex(list.arraySize - 1);
            element.FindPropertyRelative("Id").intValue = id;
            element.FindPropertyRelative("Type").stringValue = type;
            so.ApplyModifiedProperties();
        }

        // Приближённый контакт форм: центр одной внутри другой или любая вершина контура одной
        // внутри другой. Достаточно для авторской расстановки (точная выпуклая проверка не нужна).
        private static bool ShapesContact(NavNode a, NavNode b)
        {
            if (a.Contains(b.transform.position) || b.Contains(a.transform.position))
                return true;

            var points = new List<Vector3>();

            a.SampleOutline(points, 16, 0f);
            foreach (var p in points)
                if (b.Contains(p))
                    return true;

            b.SampleOutline(points, 16, 0f);
            foreach (var p in points)
                if (a.Contains(p))
                    return true;

            return false;
        }

        // Раздаёт стабильные EntityId нодам по мере линковки (и добавляет Entity, если его нет —
        // нода обязана быть Entity, чтобы жить в мире). Ненулевые существующие id сохраняет.
        private class IdContext
        {
            private readonly HashSet<int> m_Used = new();
            private int m_Next = 1;

            public IdContext()
            {
                foreach (var entity in Object.FindObjectsByType<Entity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (entity.EntityId != WorldManager.NoId)
                        m_Used.Add(entity.EntityId);
            }

            public int Ensure(NavNode node)
            {
                var entity = node.GetComponent<Entity>() ?? Undo.AddComponent<Entity>(node.gameObject);
                if (entity.EntityId != WorldManager.NoId)
                    return entity.EntityId;

                while (m_Used.Contains(m_Next))
                    m_Next++;

                Undo.RecordObject(entity, "Assign EntityId");
                entity.EntityId = m_Next;
                m_Used.Add(m_Next);
                EditorUtility.SetDirty(entity);
                return m_Next;
            }
        }

#endregion

#region Гизмо

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void DrawGizmo(NavNode node, GizmoType gizmoType)
        {
            var selected = (gizmoType & GizmoType.Selected) != 0;
            DrawShape(node, selected);
            DrawConnections(node);
        }

        private static void DrawShape(NavNode node, bool selected)
        {
            Handles.color = selected ? Color.yellow : new Color(0.4f, 0.7f, 1f, 0.9f);

            // Нода лежит на полу: контур пола на yOffset 0, потолок на Height.
            var top = new List<Vector3>();
            var bottom = new List<Vector3>();
            node.SampleOutline(top, 24, node.Height);
            node.SampleOutline(bottom, 24, 0f);

            DrawLoop(top);
            DrawLoop(bottom);
            for (var i = 0; i < top.Count; i++)
                Handles.DrawLine(bottom[i], top[i]);
        }

        private static void DrawConnections(NavNode node)
        {
            var entity = node.GetComponent<Entity>();

            foreach (var connection in node.Neighbours)
            {
                var other = FindNode(connection.Id);
                if (other == null)
                    continue;

                var color = ColorFor(connection.Type);
                Handles.color = color;
                Handles.DrawLine(node.transform.position, other.transform.position);

                // Конвексную ленту рисуем один раз на пару (со стороны меньшего id).
                if (entity != null && entity.EntityId < connection.Id)
                    DrawConvexBand(node, other, color);
            }
        }

        // Цвет по типу перехода (открытая строка; неизвестный тип — как walk).
        private static Color ColorFor(string type)
        {
            return type switch
            {
                NavConnectionType.Jump => new Color(1f, 0.55f, 0.1f),
                NavConnectionType.Ladder => new Color(0.6f, 0.4f, 1f),
                NavConnectionType.Vent => new Color(0.3f, 0.2f, 0.7f),
                _ => new Color(0.2f, 1f, 0.3f), // walk / по умолчанию
            };
        }

        // Выпуклая оболочка объединённых контуров двух нод (на нижнем из уровней) — визуализация
        // "из любой точки A в любую точку B". Полупрозрачная заливка + контур.
        private static void DrawConvexBand(NavNode a, NavNode b, Color color)
        {
            var y = Mathf.Min(a.transform.position.y, b.transform.position.y);
            var points = new List<Vector3>();
            var buffer = new List<Vector3>();

            a.SampleOutline(buffer, 16, 0f);
            foreach (var p in buffer)
                points.Add(new Vector3(p.x, y, p.z));

            b.SampleOutline(buffer, 16, 0f);
            foreach (var p in buffer)
                points.Add(new Vector3(p.x, y, p.z));

            var hull = ConvexHullXZ(points, y);
            if (hull.Count < 3)
                return;

            Handles.color = new Color(color.r, color.g, color.b, 0.07f);
            Handles.DrawAAConvexPolygon(hull.ToArray());
            Handles.color = new Color(color.r, color.g, color.b, 0.35f);
            DrawLoop(hull);
        }

        private static void DrawLoop(List<Vector3> loop)
        {
            for (var i = 0; i < loop.Count; i++)
                Handles.DrawLine(loop[i], loop[(i + 1) % loop.Count]);
        }

        // Резолв id → NavNode в редакторе (реестра мира ещё нет — обходим сцену).
        private static NavNode FindNode(int id)
        {
            foreach (var entity in Object.FindObjectsByType<Entity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (entity.EntityId == id && entity.TryGetComponent<NavNode>(out var node))
                    return node;
            return null;
        }

        // Выпуклая оболочка точек по XZ (Эндрю, monotone chain), результат — на высоте y.
        private static List<Vector3> ConvexHullXZ(List<Vector3> input, float y)
        {
            var pts = new List<Vector2>();
            foreach (var p in input)
                pts.Add(new Vector2(p.x, p.z));

            pts.Sort((l, r) => Mathf.Approximately(l.x, r.x) ? l.y.CompareTo(r.y) : l.x.CompareTo(r.x));

            var n = pts.Count;
            var result = new List<Vector3>();
            if (n < 3)
            {
                foreach (var p in pts)
                    result.Add(new Vector3(p.x, y, p.y));
                return result;
            }

            var hull = new Vector2[2 * n];
            var k = 0;

            for (var i = 0; i < n; i++)
            {
                while (k >= 2 && Cross(hull[k - 2], hull[k - 1], pts[i]) <= 0)
                    k--;
                hull[k++] = pts[i];
            }

            for (int i = n - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(hull[k - 2], hull[k - 1], pts[i]) <= 0)
                    k--;
                hull[k++] = pts[i];
            }

            for (var i = 0; i < k - 1; i++) // последняя точка == первой, отбрасываем
                result.Add(new Vector3(hull[i].x, y, hull[i].y));
            return result;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }

        private static IEnumerable<NavNode> AllNodes()
        {
            return Object.FindObjectsByType<NavNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

#endregion
    }
}
