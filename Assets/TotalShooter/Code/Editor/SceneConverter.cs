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
    }
}
