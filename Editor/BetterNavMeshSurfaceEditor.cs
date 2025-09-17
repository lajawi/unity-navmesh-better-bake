using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEngine;

namespace Lajawi
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshSurface))]
    public class BetterNavMeshSurfaceEditor : Editor
    {
        private Type _navMeshSurfaceEditor;
        private Editor _defaultEditor;
        private Type _contentType;

        private static List<GameObject> _treeParents = new();

        SerializedProperty m_AgentTypeID;
        SerializedProperty m_UseGeometry;

        void OnEnable()
        {
            _navMeshSurfaceEditor = Type.GetType("Unity.AI.Navigation.Editor.NavMeshSurfaceEditor, Unity.AI.Navigation.Editor");
            _defaultEditor = CreateEditor(targets, _navMeshSurfaceEditor);
            _contentType = Type.GetType("Unity.AI.Navigation.Editor.NavMeshSurfaceEditor, Unity.AI.Navigation.Editor").GetNestedType("Content", BindingFlags.NonPublic | BindingFlags.Static);

            m_UseGeometry = (SerializedProperty)_navMeshSurfaceEditor.GetField("m_UseGeometry", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_defaultEditor);
            m_AgentTypeID = (SerializedProperty)_navMeshSurfaceEditor.GetField("m_AgentTypeID", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_defaultEditor);
        }

        public override void OnInspectorGUI()
        {
            _defaultEditor.OnInspectorGUI();

            EditorGUILayout.Space();
            GUILayout.Label("Better NavMesh Surface", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Better NavMesh Surface Bake package is active.\nUse bellow buttons to include terrain trees.", MessageType.Info);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(Application.isPlaying || m_AgentTypeID.intValue == -1))
            {
                GUILayout.BeginHorizontal();

                using (new EditorGUI.DisabledScope(targets.All(s => (s as NavMeshSurface)?.navMeshData == null)))
                {
                    if (GUILayout.Button((GUIContent)_contentType.GetField("ClearButton").GetValue(null)))
                    {
                        NavMeshAssetManager.instance.ClearSurfaces(targets);
                        SceneView.RepaintAll();
                    }
                }

                GUIContent bakeButton = new GUIContent((GUIContent)_contentType.GetField("BakeButton").GetValue(null));
                bakeButton.text += " Better";
                bakeButton.tooltip += "\nThis button will include terrain trees.";

                if (GUILayout.Button(bakeButton))
                {
                    PreBake();
                    NavMeshAssetManager.instance.StartBakingSurfaces(targets);
                    PostBake();
                }

                GUILayout.EndHorizontal();
            }
        }

        private void PreBake()
        {
            Terrain[] terrains = Terrain.activeTerrains;

            foreach (Terrain terrain in terrains)
            {
                var treeInstances = terrain.terrainData.treeInstances;

                var parent = new GameObject($"TREES {terrain.name}");
                parent.hideFlags = HideFlags.HideAndDontSave;
                _treeParents.Add(parent);

                foreach (TreeInstance tree in treeInstances)
                {
                    GameObject treePrefab = terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab;

                    List<Component> components = new();
                    switch (m_UseGeometry.intValue)
                    {
                        case 0: // Render Meshes
                            if (treePrefab.TryGetComponent(out LODGroup lodGroup))
                            {
                                bool success = UseLODGroup(terrain, parent, tree, treePrefab, lodGroup);
                                if (success) break;

                                Debug.LogWarning($"Falling back to {nameof(MeshRenderer)} and {nameof(MeshFilter)}.", treePrefab);
                            }

                            if (!treePrefab.TryGetComponent(out MeshFilter meshFilter))
                            {
                                Debug.LogWarning($"There is no {nameof(MeshFilter)} attached to {treePrefab.name}, skipping. If you want this tree to be included in the bake, please add one.", treePrefab);
                                continue;
                            }
                            if (!treePrefab.TryGetComponent(out MeshRenderer meshRenderer))
                            {
                                Debug.LogWarning($"There is no {nameof(MeshRenderer)} attached to {treePrefab.name}, skipping. If you want this tree to be included in the bake, please add one", treePrefab);
                                continue;
                            }

                            if (!meshRenderer.enabled) Debug.LogWarning($"{treePrefab.name}'s {nameof(MeshRenderer)} is disabled.", treePrefab);

                            components.Add(meshFilter);
                            components.Add(meshRenderer);
                            break;

                        case 1: // Physics Colliders
                            if (!treePrefab.TryGetComponent(out Collider _))
                            {
                                Debug.LogWarning($"There is no {nameof(Collider)} attached to {treePrefab.name}, skipping. If you want this tree to be included in the bake, please add one.", treePrefab);
                                continue;
                            }

                            Collider[] colliders = treePrefab.GetComponents<Collider>();

                            colliders.Where(coll => !coll.enabled).ToList().ForEach(coll => Debug.LogWarning($"{treePrefab.name}'s {coll.GetType().Name} is disabled.", treePrefab));

                            components.AddRange(colliders);
                            break;
                    }

                    if (treePrefab.TryGetComponent(out NavMeshModifier modifier))
                    {
                        if (!modifier.enabled) Debug.LogWarning($"{treePrefab.name}'s {nameof(NavMeshModifier)} is disabled.", treePrefab);
                        components.Add(modifier);
                    }
                    if (treePrefab.TryGetComponent(out NavMeshModifierVolume modifierVolume))
                    {
                        if (!modifierVolume.enabled) Debug.LogWarning($"{treePrefab.name}'s {nameof(NavMeshModifierVolume)} is disabled.", treePrefab);
                        components.Add(modifierVolume);
                    }

                    if (components.Count > 0)
                    {
                        GameObject obj = CreateObject(terrain, tree, treePrefab, parent.transform);
                        AddComponents(obj, components.ToArray());
                    }
                }
            }
        }

        private static bool UseLODGroup(Terrain terrain, GameObject parent, TreeInstance tree, GameObject treePrefab, LODGroup lodGroup)
        {
            if (lodGroup.GetLODs().Length <= 0)
            {
                Debug.Log($"{treePrefab.name} has an {nameof(LODGroup)} but no LODs set up.", treePrefab);
                return false;
            }

            GameObject lodParent = CreateObject(terrain, tree, treePrefab, parent.transform);

            foreach (Renderer renderer in lodGroup.GetLODs()[0].renderers)
            {
                if (!renderer)
                {
                    Debug.LogWarning($"The {nameof(LODGroup)} LOD 0 has an empty or missing item.", lodGroup);
                    continue;
                }
                if (!renderer.gameObject.TryGetComponent(out MeshFilter lodMeshFilter))
                {
                    Debug.LogWarning($"{treePrefab.name}'s {nameof(LODGroup)} {nameof(Renderer)} lacks a {nameof(MeshFilter)}.", renderer);
                    continue;
                }
                Component[] comps = { lodMeshFilter, renderer };
                GameObject obj = CreateChild(lodParent.transform, renderer.transform);
                AddComponents(obj, comps);
            }
            if (lodGroup.GetLODs()[0].renderers.Length <= 0)
            {
                Debug.LogWarning($"{treePrefab.name} has an {nameof(LODGroup)} component, but no {nameof(Renderer)}'s on LOD 0.", treePrefab);
                return false;
            }
            if (lodParent.transform.childCount <= 0)
            {
                return false;
            }

            return true;
        }

        private static void PostBake()
        {
            foreach (GameObject parent in _treeParents)
            {
                DestroyImmediate(parent);
            }
            _treeParents = new();
        }

        private static GameObject CreateObject(Terrain terrain, TreeInstance tree, GameObject treePrefab, Transform parent)
        {
            var x = terrain.terrainData.size.x;
            var y = terrain.terrainData.size.y;
            var z = terrain.terrainData.size.z;

            Vector3 position = new Vector3(tree.position.x * x, tree.position.y * y, tree.position.z * z) + terrain.GetPosition();
            Quaternion rotation = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);

            float widthScale = tree.widthScale;
            float heightScale = tree.heightScale;
            Vector3 scale = treePrefab.transform.localScale;
            scale = new(scale.x * widthScale, scale.y * heightScale, scale.z * widthScale);

            GameObject obj = new GameObject($"Tree");
            obj.transform.parent = parent;
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.transform.localScale = scale;
            return obj;
        }

        private static GameObject CreateChild(Transform parent, Transform transform)
        {
            GameObject obj = new GameObject("Tree");
            obj.transform.parent = parent;
            obj.transform.localPosition = transform.localPosition;
            obj.transform.localRotation = transform.localRotation;
            obj.transform.localScale = transform.localScale;
            return obj;
        }

        private static void AddComponents(GameObject obj, Component[] components)
        {
            foreach (Component component in components)
            {
                var comp = obj.AddComponent(component.GetType());
                EditorUtility.CopySerialized(component, comp);
            }
        }
    }
}