using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Lajawi
{
    public static class BetterNavMeshSurfaceHook
    {
        static TreeInstance[] obstacle;
        static Terrain terrain;
        static float width;
        static float lenght;
        static float hight;
        static bool isError;
        private static GameObject trees = null;
        public static void OnPreBake(NavMeshSurface surface)
        {
            DestroyTrees();

            terrain = Terrain.activeTerrain;
            obstacle = terrain.terrainData.treeInstances;

            lenght = terrain.terrainData.size.z;
            width = terrain.terrainData.size.x;
            hight = terrain.terrainData.size.y;
            Debug.Log("Terrain Size is :" + width + " , " + hight + " , " + lenght);

            int i = 0;
            trees = new GameObject("Tree_Obstacles");

            Debug.Log("Adding " + obstacle.Length + " navMeshObstacle Components for Trees");
            foreach (TreeInstance tree in obstacle)
            {
                Vector3 tempPos = new Vector3(tree.position.x * width, tree.position.y * hight, tree.position.z * lenght);
                Quaternion tempRot = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);

                GameObject obs = new GameObject("Obstacle" + i);
                obs.transform.SetParent(trees.transform);
                obs.transform.position = tempPos;
                obs.transform.rotation = tempRot;

                obs.AddComponent<NavMeshObstacle>();
                NavMeshObstacle obsElement = obs.GetComponent<NavMeshObstacle>();
                obsElement.carving = true;
                obsElement.carveOnlyStationary = true;

                if (terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.GetComponent<Collider>() == null)
                {
                    isError = true;
                    Debug.LogError("ERROR  There is no CapsuleCollider or BoxCollider attached to ''" + terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.name + "'' please add one of them.");
                    break;
                }
                Collider coll = terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.GetComponent<Collider>();
                if (coll.GetType() == typeof(CapsuleCollider) || coll.GetType() == typeof(BoxCollider))
                {

                    if (coll.GetType() == typeof(CapsuleCollider))
                    {
                        CapsuleCollider capsuleColl = terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.GetComponent<CapsuleCollider>();
                        obsElement.shape = NavMeshObstacleShape.Capsule;
                        obsElement.center = capsuleColl.center;
                        obsElement.radius = capsuleColl.radius;
                        obsElement.height = capsuleColl.height;

                    }
                    else if (coll.GetType() == typeof(BoxCollider))
                    {
                        BoxCollider boxColl = terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.GetComponent<BoxCollider>();
                        obsElement.shape = NavMeshObstacleShape.Box;
                        obsElement.center = boxColl.center;
                        obsElement.size = boxColl.size;
                    }

                }
                else
                {
                    isError = true;
                    Debug.LogError("ERROR  There is no CapsuleCollider or BoxCollider attached to ''" + terrain.terrainData.treePrototypes[tree.prototypeIndex].prefab.name + "'' please add one of them.");
                    break;
                }


                i++;
            }
            trees.transform.position = terrain.GetPosition();
            if (!isError) Debug.Log("All " + obstacle.Length + " NavMeshObstacles were succesfully added to your Scene, Horray !");
        }

        public static void OnPostBake(NavMeshSurface surface)
        {
        }

        public static void DestroyTrees()
        {
            Object.DestroyImmediate(trees);
            trees = null;
        }
    }
}
