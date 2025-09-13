using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.AI;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;

namespace Lajawi
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshSurface))]
    public class BetterNavMeshSurfaceEditor : Editor
    {
        private static List<GameObject> treeParents = new();

        #region Variables
        SerializedProperty m_AgentTypeID;
        SerializedProperty m_DefaultArea;
        SerializedProperty m_GenerateLinks;
        SerializedProperty m_UseGeometry;

        SerializedProperty m_CollectObjects;
        SerializedProperty m_Size;
        SerializedProperty m_Center;

        SerializedProperty m_LayerMask;

        SerializedProperty m_OverrideVoxelSize;
        SerializedProperty m_VoxelSize;

        SerializedProperty m_OverrideTileSize;
        SerializedProperty m_TileSize;

        SerializedProperty m_MinRegionArea;
        SerializedProperty m_BuildHeightMesh;

        SerializedProperty m_NavMeshData;
        #endregion

        private bool IsEditingCollider
        {
            get
            {
                return EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this);
            }
        }

        void OnEnable()
        {
            m_AgentTypeID = serializedObject.FindProperty("m_AgentTypeID");
            m_DefaultArea = serializedObject.FindProperty("m_DefaultArea");
            m_GenerateLinks = serializedObject.FindProperty("m_GenerateLinks");
            m_UseGeometry = serializedObject.FindProperty("m_UseGeometry");

            m_CollectObjects = serializedObject.FindProperty("m_CollectObjects");
            m_Size = serializedObject.FindProperty("m_Size");
            m_Center = serializedObject.FindProperty("m_Center");

            m_LayerMask = serializedObject.FindProperty("m_LayerMask");

            m_OverrideVoxelSize = serializedObject.FindProperty("m_OverrideVoxelSize");
            m_VoxelSize = serializedObject.FindProperty("m_VoxelSize");

            m_OverrideTileSize = serializedObject.FindProperty("m_OverrideTileSize");
            m_TileSize = serializedObject.FindProperty("m_TileSize");

            m_MinRegionArea = serializedObject.FindProperty("m_MinRegionArea");
            m_BuildHeightMesh = serializedObject.FindProperty("m_BuildHeightMesh");

            m_NavMeshData = serializedObject.FindProperty("m_NavMeshData");
        }

        Bounds GetBounds()
        {
            var navSurface = (NavMeshSurface)target;
            return new Bounds(navSurface.transform.position, navSurface.size);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var bs = NavMesh.GetSettingsByID(m_AgentTypeID.intValue);

            #region Graphic
            if (bs.agentTypeID != -1)
            {
                // Draw image
                const float diagramHeight = 80.0f;
                Rect agentDiagramRect = EditorGUILayout.GetControlRect(false, diagramHeight);
                NavMeshEditorHelpers.DrawAgentDiagram(
                    agentDiagramRect,
                    bs.agentRadius,
                    bs.agentHeight,
                    bs.agentClimb,
                    bs.agentSlope
                );
            }
            #endregion

            #region Agent Type
            NavMeshComponentsGUIUtility.AgentTypePopup(Content.AgentType.ToString(), m_AgentTypeID);
            #endregion

            #region Default Area
            NavMeshComponentsGUIUtility.AreaPopup(Content.DefaultArea.ToString(), m_DefaultArea);
            #endregion

            #region Generate Links
            EditorGUILayout.PropertyField(m_GenerateLinks, Content.GenerateLinks);
            #endregion

            #region Use Geometry
            EditorGUILayout.PropertyField(m_UseGeometry, Content.UseGeometry);
            #endregion

            #region Object Collection
            m_CollectObjects.isExpanded = EditorGUILayout.Foldout(m_CollectObjects.isExpanded, Content.ObjectCollectionHeader, true);

            if (m_CollectObjects.isExpanded)
            {
                EditorGUI.indentLevel++;

                #region Collect Objects
                EditorGUILayout.PropertyField(m_CollectObjects, Content.CollectObjects);

                #region Volume
                if ((CollectObjects)m_CollectObjects.enumValueIndex == CollectObjects.Volume)
                {
                    EditorGUI.indentLevel++;

                    EditMode.DoEditModeInspectorModeButton(EditMode.SceneViewEditMode.Collider, "Edit Volume", EditorGUIUtility.IconContent("EditCollider"), GetBounds, this);
                    EditorGUILayout.PropertyField(m_Size);
                    EditorGUILayout.PropertyField(m_Center);

                    EditorGUI.indentLevel--;
                }
                else
                {
                    if (IsEditingCollider)
                        EditMode.QuitEditMode();
                }
                #endregion

                #region Include Layers
                EditorGUILayout.PropertyField(m_LayerMask, Content.IncludeLayers);
                #endregion
                #endregion

                EditorGUI.indentLevel--;
            }
            #endregion

            EditorGUILayout.Space();

            #region Advanced
            m_OverrideVoxelSize.isExpanded = EditorGUILayout.Foldout(m_OverrideVoxelSize.isExpanded, Content.AdvancedHeader, true);
            if (m_OverrideVoxelSize.isExpanded)
            {
                EditorGUI.indentLevel++;

                #region Override Voxel Size
                // Override voxel size.
                EditorGUILayout.PropertyField(m_OverrideVoxelSize, Content.OverrideVoxelSize);

                using (new EditorGUI.DisabledScope(!m_OverrideVoxelSize.boolValue || m_OverrideVoxelSize.hasMultipleDifferentValues))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(m_VoxelSize, Content.VoxelSize);

                    if (!m_OverrideVoxelSize.hasMultipleDifferentValues)
                    {
                        if (!m_AgentTypeID.hasMultipleDifferentValues)
                        {
                            float voxelsPerRadius = m_VoxelSize.floatValue > 0.0f ? (bs.agentRadius / m_VoxelSize.floatValue) : 0.0f;
                            EditorGUILayout.LabelField(" ", string.Format(Content.VoxelSizeFormatString, voxelsPerRadius), EditorStyles.miniLabel);
                        }

                        if (m_OverrideVoxelSize.boolValue)
                            EditorGUILayout.HelpBox(Content.VoxelSizeHelpBox, MessageType.None);
                    }

                    EditorGUI.indentLevel--;
                }
                #endregion

                #region Override Tile Size
                EditorGUILayout.PropertyField(m_OverrideTileSize, Content.OverrideTileSize);

                using (new EditorGUI.DisabledScope(!m_OverrideTileSize.boolValue || m_OverrideTileSize.hasMultipleDifferentValues))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(m_TileSize, Content.TileSize);

                    if (!m_TileSize.hasMultipleDifferentValues && !m_VoxelSize.hasMultipleDifferentValues)
                    {
                        float tileWorldSize = m_TileSize.intValue * m_VoxelSize.floatValue;
                        EditorGUILayout.LabelField(" ", string.Format(Content.TileWorldSizeFormatString, tileWorldSize), EditorStyles.miniLabel);
                    }

                    if (!m_OverrideTileSize.hasMultipleDifferentValues)
                    {
                        if (m_OverrideTileSize.boolValue)
                            EditorGUILayout.HelpBox(Content.TileWorldSizeHelpBox, MessageType.None);
                    }

                    EditorGUI.indentLevel--;
                }
                #endregion

                #region Minimum Region Area
                EditorGUILayout.PropertyField(m_MinRegionArea, Content.MinimumRegionArea);
                #endregion

                #region Build Height Mesh
                EditorGUILayout.PropertyField(m_BuildHeightMesh, Content.BuildHeightMesh);
                #endregion

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }
            #endregion

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            var hadError = false;
            var multipleTargets = targets.Length > 1;
            foreach (NavMeshSurface navSurface in targets)
            {
                var settings = navSurface.GetBuildSettings();

                // Calculating bounds is potentially expensive when unbounded - so here we just use the center/size.
                // It means the validation is not checking vertical voxel limit correctly when the surface is set to something else than "in volume".
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                if (navSurface.collectObjects == CollectObjects.Volume)
                {
                    bounds = new Bounds(navSurface.center, navSurface.size);
                }

                /* var errors = settings.ValidationReport(bounds);
                if (errors.Length > 0)
                {
                    if (multipleTargets)
                        EditorGUILayout.LabelField(navSurface.name);
                    foreach (var err in errors)
                    {
                        EditorGUILayout.HelpBox(err, MessageType.Warning);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    if (GUILayout.Button(NavMeshComponentsGUIUtility.k_OpenAgentSettingsText, EditorStyles.miniButton))
                        NavMeshEditorHelpers.OpenAgentSettings(navSurface.agentTypeID);
                    GUILayout.EndHorizontal();
                    hadError = true;
                } */
            }

            if (hadError)
                EditorGUILayout.Space();

            #region NavMesh Data
            var nmdRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(nmdRect, GUIContent.none, m_NavMeshData);
            var rectLabel = EditorGUI.PrefixLabel(nmdRect, GUIUtility.GetControlID(FocusType.Passive), Content.NavMeshData);
            EditorGUI.EndProperty();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.BeginProperty(nmdRect, GUIContent.none, m_NavMeshData);
                EditorGUI.ObjectField(rectLabel, m_NavMeshData, GUIContent.none);
                EditorGUI.EndProperty();
            }
            #endregion

            #region Buttons
            using (new EditorGUI.DisabledScope(Application.isPlaying || m_AgentTypeID.intValue == -1))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);

                #region Clear
                using (new EditorGUI.DisabledScope(targets.All(s => (s as NavMeshSurface)?.navMeshData == null)))
                {
                    if (GUILayout.Button(Content.ClearButton))
                    {
                        NavMeshAssetManager.instance.ClearSurfaces(targets);
                        SceneView.RepaintAll();
                    }
                }
                #endregion

                #region Bake
                if (GUILayout.Button(Content.BakeButton))
                {
                    PreBake();
                    NavMeshAssetManager.instance.StartBakingSurfaces(targets);
                    PostBake();
                }
                #endregion

                GUILayout.EndHorizontal();
            }
            #endregion

            EditorGUILayout.HelpBox("Better NavMesh Surface Bake package is active", MessageType.Info);
        }

        private void PreBake()
        {
            Terrain[] terrains = Terrain.activeTerrains;

            foreach (Terrain terrain in terrains)
            {
                var treeInstances = terrain.terrainData.treeInstances;

                var x = terrain.terrainData.size.x;
                var y = terrain.terrainData.size.y;
                var z = terrain.terrainData.size.z;

                var parent = new GameObject($"TREES {terrain.name}");
                treeParents.Add(parent);

                foreach (TreeInstance tree in treeInstances)
                {
                    GameObject treePrefab = terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab;

                    List<Component> components = new();
                    switch (m_UseGeometry.intValue)
                    {
                        case 0: // Render Meshes
                            MeshFilter meshFilter = treePrefab.GetComponent<MeshFilter>();
                            if (meshFilter == null)
                            {
                                Debug.LogWarning($"There is no mesh filter attached to {treePrefab.name}, skipping. If you want this tree to be included in the bake, please add one.", treePrefab);
                                continue;
                            }
                            MeshRenderer meshRenderer = treePrefab.GetComponent<MeshRenderer>();
                            if (meshRenderer == null)
                            {
                                Debug.LogWarning($"There is no mesh renderer attached to {treePrefab.name}, skipping. If you want this tree to be included in the bake, please add one", treePrefab);
                                continue;
                            }
                            components.Add(meshFilter);
                            components.Add(meshRenderer);
                            break;

                        case 1: // Physics Colliders
                            Collider collider = treePrefab.GetComponent<Collider>();
                            if (collider == null)
                            {
                                Debug.LogWarning($"There is no collider attached to {treePrefab.name}, skipping. If you want this tree to be included in the bake, please add one.", treePrefab);
                                continue;
                            }
                            components.Add(collider);
                            break;
                    }

                    Vector3 position = new Vector3(tree.position.x * x, tree.position.y * y, tree.position.z * z) + terrain.GetPosition();
                    Quaternion rotation = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);

                    float widthScale = tree.widthScale;
                    float heightScale = tree.heightScale;
                    Vector3 scale = treePrefab.transform.localScale;
                    scale = new(scale.x * widthScale, scale.y * heightScale, scale.z * widthScale);

                    GameObject obj = new GameObject($"Tree");
                    obj.transform.parent = parent.transform;
                    obj.transform.position = position;
                    obj.transform.rotation = rotation;
                    obj.transform.localScale = scale;

                    AddComponents(obj, components.ToArray());
                }
            }
        }

        private void PostBake()
        {
            foreach (GameObject parent in treeParents)
            {
                DestroyImmediate(parent);
            }
            treeParents = new();
        }

        private void AddComponents(GameObject obj, Component[] components)
        {
            foreach (Component component in components)
            {
                var comp = obj.AddComponent(component.GetType());
                EditorUtility.CopySerialized(component, comp);
            }
        }

        static class Content
        {
            public static readonly GUIContent AgentType = EditorGUIUtility.TrTextContent("Agent Type", "The NavMesh Agent type that uses the NavMesh Surface.");
            public static readonly GUIContent DefaultArea = EditorGUIUtility.TrTextContent("Default Area", "The area type assumed for all the objects at the moment when Unity generates the NavMesh. Use the NavMesh Modifier component to override the area type of an object and its hierarchy.");
            public static readonly GUIContent GenerateLinks = EditorGUIUtility.TrTextContent("Generate Links", "If enabled, collected objects will generate unidirectional links according to the drop height and jump distance values in the agent settings. Use the NavMesh Modifier component to override this behavior for an object and its hierarchy.");
            public static readonly GUIContent UseGeometry = EditorGUIUtility.TrTextContent("Use Geometry", "The type of geometry to create the NavMesh from.");
            public static readonly GUIContent ObjectCollectionHeader = EditorGUIUtility.TrTextContent("Object Collection", "Parameters that define how to select objects from the scene.");
            public static readonly GUIContent CollectObjects = EditorGUIUtility.TrTextContent("Collect Objects", "Defines which GameObjects to use for baking.");
            public static readonly GUIContent IncludeLayers = EditorGUIUtility.TrTextContent("Include Layers", "Define the layers on which GameObjects are included in the bake process.");
            public static readonly GUIContent AdvancedHeader = EditorGUIUtility.TrTextContent("Advanced", "Parameters that control the level of detail and the structure of the navigation data during its creation.");
            public static readonly GUIContent OverrideVoxelSize = EditorGUIUtility.TrTextContent("Override Voxel Size", "If enabled, uses the value below to control how accurately Unity processes the scene geometry when it creates the NavMesh.");
            public static readonly GUIContent VoxelSize = EditorGUIUtility.TrTextContent("Voxel Size", "The width of cells in a grid used to sample the level geometry. The cell height is half of the width.");
            public static readonly string VoxelSizeFormatString = L10n.Tr("{0:0.00} voxels per agent radius");
            public static readonly string VoxelSizeHelpBox = L10n.Tr("Voxel size controls the accuracy with which Unity generates the NavMesh from the scene geometry. A good voxel size fits 2-4 voxels per agent radius. When you reduce the voxel size, both the accuracy and the bake duration increase.");
            public static readonly GUIContent OverrideTileSize = EditorGUIUtility.TrTextContent("Override Tile Size", "If enabled, the value below overrides the size of the tiles that partition the NavMesh.");
            public static readonly GUIContent TileSize = EditorGUIUtility.TrTextContent("Tile Size", "The number of voxels that determines the width of a square NavMesh tile. The created NavMesh is subdivided into a grid of tiles in order to make the bake process parallel and memory efficient. A value of 256 is a good balance between memory usage and NavMesh fragmentation.");
            public static readonly string TileWorldSizeFormatString = L10n.Tr("{0:0.00} world units");
            public static readonly string TileWorldSizeHelpBox = L10n.Tr("Tile size reduces the impact of scene changes to only a section of the NavMesh. Smaller tiles allow carving or rebuilding to produce localized changes but may generate more polygon data overall.");
            public static readonly GUIContent MinimumRegionArea = EditorGUIUtility.TrTextContent("Minimum Region Area", "Allows you to cull away the small regions disconnected from the larger NavMesh.");
            public static readonly GUIContent BuildHeightMesh = EditorGUIUtility.TrTextContent("Build Height Mesh", "Enables the creation of additional data used for determining more accurately the height at any position on the NavMesh.");
            public static readonly GUIContent NavMeshData = EditorGUIUtility.TrTextContent("NavMesh Data", "Locate the asset file where the NavMesh is stored.");
            public static readonly GUIContent ClearButton = EditorGUIUtility.TrTextContent("Clear", "Clear NavMesh data for this surface.");
            public static readonly GUIContent BakeButton = EditorGUIUtility.TrTextContent("Bake", "Create the NavMesh with the current settings.");
            public static readonly GUIContent BakeInProgressButton = EditorGUIUtility.TrTextContent("NavMesh baking is in progress.");
            public static readonly string UndoModifyVolume = L10n.Tr("Modify NavMesh Surface Volume");
        }
    }
}