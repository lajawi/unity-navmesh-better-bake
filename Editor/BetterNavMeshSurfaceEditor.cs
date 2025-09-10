#if UNITY_EDITOR
using System;
using Lajawi;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavMeshSurface))]
[CanEditMultipleObjects]
public class BetterNavMeshSurfaceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Clear"))
            {
                ClearAll();
            }
            if (GUILayout.Button("Bake"))
            {
                BakeAll();
            }
        }

        EditorGUILayout.HelpBox("Better NavMesh Surface package is active.\nTerrain Trees will be included.", MessageType.Info);
    }

    private void BakeAll()
    {
        foreach (var obj in targets)
        {
            var surface = obj as NavMeshSurface;
            if (surface == null) continue;

            // var hooks = surface.GetComponents<BetterNavMeshSurfaceHook>();
            try
            {
                SafeInvoke(() => BetterNavMeshSurfaceHook.OnPreBake(surface), "OnPreBake");

                surface.BuildNavMesh();

                SafeInvoke(() => BetterNavMeshSurfaceHook.OnPostBake(surface), "OnPostBake");
            }
            catch (Exception ex)
            {
                Debug.LogError($"NavMesh bake failed on {surface.name}: {ex}");
            }
        }
    }

    private void ClearAll()
    {
        foreach (var obj in targets)
        {
            var surface = obj as NavMeshSurface;
            if (surface == null) continue;

            Undo.RecordObject(surface, "Clear NavMesh Data");
            surface.RemoveData();
            EditorUtility.SetDirty(surface);
        }

        SafeInvoke(() => BetterNavMeshSurfaceHook.DestroyTrees(), "DestroyTrees");
    }

    private static void SafeInvoke(Action call, string label)
    {
        Debug.Log($"Calling {label}");
        try { call(); }
        catch (Exception ex)
        {
            Debug.LogError($"Error during {label}: {ex}");
        }
    }
}
#endif
