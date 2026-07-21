using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sadalmalik.TotalShooter.Editor
{
    // Editor-тулза: сохраняет все Entity текущей сцены как файл мира (world.json). Черновой
    // способ авторить первые версии миров в Unity-редакторе, пока нет внутриигрового редактора.
    public static class SceneConverter
    {
        [MenuItem("Tools/SaveAsWorld")]
        public static void SaveAsWorld()
        {
            var entities = Object.FindObjectsByType<Entity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (entities.Length == 0)
            {
                EditorUtility.DisplayDialog("Save As World", "На сцене нет ни одного Entity.", "OK");
                return;
            }

            var folder = EditorUtility.SaveFolderPanel("Save Scene As World", WorldManager.WorldsRoot, "");
            if (string.IsNullOrEmpty(folder))
                return;

            AssignIds(entities);
            AssignProtos(entities);

            var json = WorldManager.SerializeEntities(entities);
            File.WriteAllText(Path.Combine(folder, "world.json"), json);

            Debug.Log($"Saved {entities.Length} entities to {folder}/world.json");
        }

        // Гарантирует уникальные EntityId: сохраняет уже проставленные (не 0, без коллизий),
        // остальным выдаёт свежие. Пишет назначенные id обратно в объекты сцены (через Undo +
        // dirty), чтобы при повторном экспорте id были стабильны.
        private static void AssignIds(IReadOnlyList<Entity> entities)
        {
            var used = new HashSet<int>();
            var needId = new List<Entity>();

            foreach (var entity in entities)
            {
                if (entity.EntityId != WorldManager.NoId && used.Add(entity.EntityId))
                    continue;
                needId.Add(entity);
            }

            var next = 1;
            foreach (var entity in needId)
            {
                while (used.Contains(next))
                    next++;

                Undo.RecordObject(entity, "Assign EntityId");
                entity.EntityId = next;
                used.Add(next);
                EditorUtility.SetDirty(entity);
            }

            if (needId.Count > 0)
                EditorSceneManager.MarkAllScenesDirty();
        }

        // Для корней префаб-инстансов проставляет Proto = путь префаба относительно папки Resources
        // (без расширения) — по нему WorldManager при загрузке переинстанцирует базу через
        // Resources.Load, а сверху накладывает сохранённые оверрайды. Только корни инстансов
        // (не дети внутри инстанса) и только префабы, лежащие в Resources.
        private static void AssignProtos(IReadOnlyList<Entity> entities)
        {
            var changed = false;

            foreach (var entity in entities)
            {
                var go = entity.gameObject;
                if (PrefabUtility.GetNearestPrefabInstanceRoot(go) != go)
                    continue;

                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                var proto = ToResourcesPath(assetPath);
                if (proto == null)
                {
                    Debug.LogWarning($"'{go.name}' — префаб-инстанс вне папки Resources ({assetPath}), " +
                                     "Proto не проставлен (WorldManager грузит прототипы через Resources).", go);
                    continue;
                }

                if (entity.Proto == proto)
                    continue;

                Undo.RecordObject(entity, "Assign Proto");
                entity.Proto = proto;
                EditorUtility.SetDirty(entity);
                changed = true;
            }

            if (changed)
                EditorSceneManager.MarkAllScenesDirty();
        }

        // "Assets/.../Resources/Props/Barrel.prefab" → "Props/Barrel". Вне Resources → null.
        private static string ToResourcesPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            const string marker = "/Resources/";
            var index = assetPath.IndexOf(marker, System.StringComparison.Ordinal);
            if (index < 0)
                return null;

            var relative = assetPath.Substring(index + marker.Length);
            var dot = relative.LastIndexOf('.');
            return dot >= 0 ? relative.Substring(0, dot) : relative;
        }
    }
}
