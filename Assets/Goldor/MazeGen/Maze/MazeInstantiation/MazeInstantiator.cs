using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MazeGen
{
    [CreateAssetMenu(fileName = "MazeInstancier", menuName = "MazeGen/Maze Config/Instancier")]
    public class MazeInstantiator : ScriptableObject
    {
        [Tooltip("The object used for the forward path")] public GameObject forwardPath;
        [Tooltip("The object used for the path turning right")] public GameObject cornerRightPath;
        [Tooltip("The object used for the path turning left")] public GameObject cornerLeftPath;
        [Tooltip("The object used for the path splitting")] public GameObject splitPath;
        [Tooltip("The object used for the path connecting 4 path")] public GameObject crossPath;
        [Tooltip("The object used for the last part closing a path")] public GameObject endPath;
        [Tooltip("The object used for last part of the maze closing a path")] public GameObject mazeEnd;
        [Tooltip("Size of the parts")] public float size;
        [Tooltip("Size of the batching")] public int groupResolution;

        // assign prefabs to id of the maze representation
        private void UpdateRegex(MazeRegistry registry)
        {
            registry.SetPrimordial((int) MazePart.PrimordialPart.MazeEnd, mazeEnd);
            registry.SetPrimordial((int) MazePart.PrimordialPart.PathEnd, endPath);
            registry.SetPrimordial((int) MazePart.PrimordialPart.StraightPath, forwardPath);
            registry.SetPrimordial((int) MazePart.PrimordialPart.CornerRightPath, cornerRightPath);
            registry.SetPrimordial((int) MazePart.PrimordialPart.CornerLeftPath, cornerLeftPath);
            registry.SetPrimordial((int) MazePart.PrimordialPart.TPath, splitPath);
            registry.SetPrimordial((int) MazePart.PrimordialPart.XPath, crossPath);
        }

        /// <summary>
        /// Instantiate a maze
        /// </summary>
        /// <param name="maze">Maze holding the raw generation</param>
        /// <param name="registry">Link between maze id and gameobject</param>
        /// <param name="rootObject">Object that hold the final maze</param>
        /// <param name="bakeImmediate">Specify whether or not if maze should be baked during instantiation</param>
        /// <param name="bakedMeshes">Put mesh that have been baked in the list (it need to be destroy manually when deleting the maze to avoid memory leak)</param>
        public void InstantiateMaze(MazeContainer maze, MazeRegistry registry, Transform rootObject, bool bakeImmediate, [CanBeNull] List<MeshFilter> bakedMeshes)
        {
            UpdateRegex(registry);
            DateTime startTime = DateTime.Now;
            Debug.Log("Starting maze instantiation at : " + startTime);

            DestroyChild(rootObject.gameObject);

            GameObject[,] groups = null;
            if (groupResolution > 0)
            {
                int xGroup = maze.SizeX / groupResolution;
                int yGroup = maze.SizeY / groupResolution;
                if(maze.SizeX % groupResolution > 0)
                    xGroup++;
                if(maze.SizeY % groupResolution > 0)
                    yGroup++;

                groups = new GameObject[xGroup, yGroup];
            
                for (int groupX = 0; groupX < xGroup; groupX++)
                {
                    for (int groupY = 0; groupY < yGroup; groupY++)
                    {
                        GameObject go = new GameObject("Group " + groupX + "," + groupY);
                        go.isStatic = true; //have object possible to bake
                        groups[groupX, groupY] = go;
                        go.transform.parent = rootObject;
                        go.transform.position = new Vector3((0.5f + groupX) * groupResolution * size, 0, (0.5f + groupY) * groupResolution * size);
                    }
                }
                
                for (int x = 0; x < xGroup; x++)
                {
                    for (int y = 0; y < yGroup; y++)
                    {
                        InstantiateGroup(x,y, registry, maze, groups[x, y].transform);
                        if(bakeImmediate) Bake(groups[x, y], bakedMeshes);
                    }
                }
            }
            else
            {
                for (int x = 0; x < maze.SizeX; x++)
                {
                    for (int y = 0; y < maze.SizeY; y++)
                    {
                        MazePart mazePart = maze.GetPart(new Vector2Int(x, y));
                        InstantiateMazePart(x, y, mazePart, registry, rootObject);
                    }
                }
            }

            Debug.Log("Maze instantiation finished at : "+DateTime.Now+" in : "+DateTime.Now.Subtract(startTime));
        }

        private void InstantiateGroup(int groupX, int groupY, MazeRegistry registry, MazeContainer maze, Transform group)
        {
            for (int x = groupX * groupResolution; x < (groupX + 1) * groupResolution; x++)
            {
                for (int y = groupY * groupResolution; y < (groupY + 1) * groupResolution; y++)
                {
                    MazePart mazePart = maze.GetPart(new Vector2Int(x, y));
                    InstantiateMazePart(x, y, mazePart, registry, group.transform);
                }
            }
        }

        /// <summary>
        /// Instantiate a maze part.
        /// </summary>
        /// <param name="x">X coordinate.</param>
        /// <param name="y">Y coordinate.</param>
        /// <param name="mazePart">Maze part to instantiate.</param>
        /// <param name="registry">Regex linking part to gameobject.</param>
        /// <param name="rootObject">Parent of the part to instantiate.</param>
        /// <returns>Return the instantiated part.</returns>
        public GameObject InstantiateMazePart(int x, int y,MazePart mazePart, MazeRegistry registry, Transform rootObject)
        {
            if (mazePart == null) return null;
            
            GameObject gameObject = registry.GetType(mazePart.part, mazePart.type);
            if (gameObject == null) return null;
            
            //direction + 1 is to transform the forward direction from x+ to z+
            Quaternion rot = Quaternion.Euler(new Vector3(0, (mazePart.direction + 1) * 90, 0));
            Vector3 pos = new Vector3(x * size, 0, y * size);
            GameObject go = Instantiate(gameObject, pos, rot, rootObject);
            go.name = $"x:{x}-y:{y} primordialPart:{mazePart.part} typeid:{mazePart.type}";
            return go;

        }

        /// <summary>
        /// Remove a part.
        /// </summary>
        /// <param name="go">Gameobject to remove.</param>
        public void RemoveMazePart(GameObject go)
        {
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }

        /// <summary>
        /// Bake direct children of the given object individually
        /// </summary>
        /// <param name="mazeRoot">parent</param>
        /// <param name="bakedMeshes">put mesh that have been baked in the list (it need to be destroy manually when deleting the maze to avoid memory leak)</param>
        public void BakeAll(GameObject mazeRoot, List<MeshFilter> bakedMeshes)
        {
            if (groupResolution > 0)
            {
                foreach (Transform child in mazeRoot.transform)
                    Bake(child.gameObject, bakedMeshes);
            }
            else
            {
                Bake(mazeRoot, bakedMeshes);
            }
        }

        /// <summary>
        /// Combine all static mesh from children into the given object
        /// </summary>
        /// <param name="obj">parent</param>
        /// /// <param name="bakedMeshes">put mesh that have been baked in the list (need to destroy them individually to avoid memory leak)</param>
        public void Bake(GameObject obj, List<MeshFilter> bakedMeshes)
        {
            (Mesh, Material[])? bakedRenderers = GatherMeshRenderer(obj);
            (Mesh, Material[])? bakedColliders = GatherMeshCollider(obj);
            (Mesh, Material[])? bakedCollidersAndRenderers = GatherMeshColliderAndRenderer(obj);
            
            DestroyStaticChild(obj);
            
            if(bakedRenderers.HasValue)
                new CombinedMeshBuilder(obj, bakedRenderers.Value.Item1, bakedMeshes, "Baked Renderers")
                    .AddRenderer(bakedRenderers.Value.Item2)
                    .Build();

            if(bakedColliders.HasValue)
                new CombinedMeshBuilder(obj, bakedColliders.Value.Item1, bakedMeshes, "Baked Colliders")
                    .AddCollider()
                    .Build();

            if(bakedCollidersAndRenderers.HasValue)
                new CombinedMeshBuilder(obj, bakedCollidersAndRenderers.Value.Item1, bakedMeshes, "Baked Colliders and Renderers")
                    .AddCollider()
                    .AddRenderer(bakedCollidersAndRenderers.Value.Item2)
                    .Build();
        }

        private (Mesh, Material[])? GatherMeshRenderer(GameObject rootObject)
        {
            List<MeshFilter> onlyMeshRenderers = new List<MeshFilter>();
            AddStaticMeshRecursivelyColliderFilterRendererFilter(rootObject, onlyMeshRenderers, true, false, true);
            
            return onlyMeshRenderers.Count <= 0 ? null : CombineMeshes(rootObject.transform.position, onlyMeshRenderers, true);
        }
        
        private (Mesh, Material[])? GatherMeshCollider(GameObject rootObject)
        {
            List<MeshFilter> onlyMeshColliders = new List<MeshFilter>();
            AddStaticMeshRecursivelyColliderFilterRendererFilter(rootObject, onlyMeshColliders, true, true, false);
            
            return onlyMeshColliders.Count <= 0 ? null : CombineMeshes(rootObject.transform.position, onlyMeshColliders, false);
        }
        
        private (Mesh, Material[])? GatherMeshColliderAndRenderer(GameObject rootObject)
        {
            List<MeshFilter> onlyMeshCollidersAndMeshRenderers = new List<MeshFilter>();
            AddStaticMeshRecursivelyColliderFilterRendererFilter(rootObject, onlyMeshCollidersAndMeshRenderers, true, true, true);
            
            return onlyMeshCollidersAndMeshRenderers.Count <= 0 ? null : CombineMeshes(rootObject.transform.position, onlyMeshCollidersAndMeshRenderers, true);
        }

        private (Mesh, Material[])? CombineMeshes(Vector3 offSet, List<MeshFilter> meshes, bool keepMaterials)
        {
            if (meshes.Count == 0)
            {
                Debug.Log("No mesh to combine cancelling baking");
                return null;
            }
            
            List<Material> materials = new List<Material>();
            var combineArrayInstances = ComputeDataForMeshCombiningByMaterial(meshes, materials, offSet, keepMaterials);
            CombineInstance[] combineInstances = MergeMeshesDatabyMaterial(combineArrayInstances);

            Mesh bakedMesh = new Mesh();
            bakedMesh.indexFormat = IndexFormat.UInt32; //allow more than 65535 vertices
            bakedMesh.CombineMeshes(combineInstances, false, false);
            
            foreach (CombineInstance combineInstance in combineInstances) //free memory
            {
                if(Application.isPlaying)
                    Destroy(combineInstance.mesh);
                else
                    DestroyImmediate(combineInstance.mesh);
            }
            
            bakedMesh.RecalculateBounds();
            bakedMesh.Optimize();

            return (bakedMesh, materials.ToArray());
        }

        private List<List<CombineInstance>> ComputeDataForMeshCombiningByMaterial(List<MeshFilter> meshes, List<Material> materials, Vector3 transformOffSet, bool keepMaterials)
        {
            List<List<CombineInstance>> combineArrayInstances = new List<List<CombineInstance>>(); //one combine array per material

            for (var meshIndex = meshes.Count - 1; meshIndex >= 0; meshIndex--) //reverse loop to avoid changing child transform when changing parent transform
            {
                MeshFilter meshFilter = meshes[meshIndex];
                if (keepMaterials)
                {
                    AddMeshDataConservingMaterials(combineArrayInstances, materials, meshFilter, transformOffSet);
                }
                else
                {
                    combineArrayInstances.Add(new List<CombineInstance>());
                    AddMeshData(combineArrayInstances[0], meshFilter, transformOffSet);
                }
            }

            return combineArrayInstances;
        }

        private static CombineInstance[] MergeMeshesDatabyMaterial(List<List<CombineInstance>> combineArrayInstances)
        {
            CombineInstance[] combineInstances = new CombineInstance[combineArrayInstances.Count]; //one combine per material
            for (int i = 0; i < combineArrayInstances.Count; i++)
            {
                Mesh materialMesh = new Mesh();
                materialMesh.indexFormat = IndexFormat.UInt32; //allow more than 65535 vertices

                materialMesh.CombineMeshes(combineArrayInstances[i].ToArray(), true, true);

                combineInstances[i] = new CombineInstance() { mesh = materialMesh, subMeshIndex = 0 };
            }

            return combineInstances;
        }

        private static void AddMeshData(List<CombineInstance> combineArrayInstance, MeshFilter meshFilter, Vector3 transformOffSet)
        {
            CombineInstance combineInstance = new CombineInstance();
            combineInstance.mesh = meshFilter.sharedMesh;
            combineInstance.subMeshIndex = 0;

            Transform meshTransform = meshFilter.transform;
            meshTransform.position -= transformOffSet;
            combineInstance.transform = meshTransform.localToWorldMatrix;

            combineArrayInstance.Add(combineInstance);
        }

        private void AddMeshDataConservingMaterials(List<List<CombineInstance>> combineArrayInstances, List<Material> materials, MeshFilter meshFilter, Vector3 transformOffSet)
        {
            MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();

            for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
            {
                int subMeshIndex = GetOrAddIndex(materials, meshRenderer.sharedMaterials[i]);
                if (subMeshIndex >= combineArrayInstances.Count)
                {
                    combineArrayInstances.Add(new List<CombineInstance>());
                }

                CombineInstance combineInstance = ExtractSubmeshDataIntoCombineInstance(meshFilter, i, transformOffSet);

                combineArrayInstances[subMeshIndex].Add(combineInstance);
            }
        }

        private static CombineInstance ExtractSubmeshDataIntoCombineInstance(MeshFilter meshFilter, int originalSubMeshIndex, Vector3 transformOffSet)
        {
            CombineInstance combineInstance = new CombineInstance();
            combineInstance.mesh = meshFilter.sharedMesh;
            combineInstance.subMeshIndex = originalSubMeshIndex;

            Transform meshTransform = meshFilter.transform;
            meshTransform.position -= transformOffSet;
            combineInstance.transform = meshTransform.localToWorldMatrix;
            return combineInstance;
        }

        private int GetOrAddIndex<T>(List<T> list, T o)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Equals(o))
                {
                    return i;
                }
            }
            list.Add(o);
            return list.Count - 1;
        }
        
        private void AddStaticMeshRecursively(GameObject go, List<MeshFilter> meshes, bool excludeCurrent)
        {
            if(!go.isStatic) return;
            
            if (!excludeCurrent)
            {
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                if(meshFilter != null)
                    meshes.Add(meshFilter);
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                AddStaticMeshRecursively(go.transform.GetChild(i).gameObject, meshes, false);
            }
        }
        
        private void AddStaticMeshRecursivelyColliderFilterRendererFilter(GameObject go, List<MeshFilter> meshes, bool excludeCurrent, bool withCollider, bool withRenderer)
        {
            if(!go.isStatic) return;
            
            if (!excludeCurrent)
            {
                MeshCollider meshCollider = go.GetComponent<MeshCollider>();
                MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                
                
                if (meshFilter != null &&
                    ((withRenderer && meshRenderer != null) || (!withRenderer && meshRenderer == null)) &&
                    ((withCollider && meshCollider != null) || (!withCollider && meshCollider == null)))
                {
                    
                    meshes.Add(meshFilter);
                }
                    
            }
            
            for (int i = 0; i < go.transform.childCount; i++)
            {
                AddStaticMeshRecursivelyColliderFilterRendererFilter(go.transform.GetChild(i).gameObject, meshes, false, withCollider, withRenderer);
            }
        }
        
        private void DestroyChild(GameObject go)
        {
            GameObject[] toDestroy = new GameObject[go.transform.childCount];
            for (int i = 0; i < go.transform.childCount; i++)
            {
                toDestroy[i] = go.transform.GetChild(i).gameObject;
            }

            if (Application.isPlaying)
            {
                foreach (GameObject gameObject in toDestroy)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                foreach (GameObject gameObject in toDestroy)
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        private bool DestroyStaticChild(GameObject go)
        {
            if (!go.isStatic) return false;
            
            List<GameObject> toDestroy = new List<GameObject>();

            bool parentOfDynamic = false;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                if (DestroyStaticChild(go.transform.GetChild(i).gameObject))
                {
                    if (go.transform.GetChild(i).gameObject.isStatic)
                        toDestroy.Add(go.transform.GetChild(i).gameObject);
                }
                else
                {
                    parentOfDynamic = true;
                }
            }

            if (Application.isPlaying)
            {
                foreach (GameObject gameObject in toDestroy)
                {
                    DestroyMeshFilterAndRenderer(gameObject);
                    Destroy(gameObject);
                }
            }
            else
            {
                foreach (GameObject gameObject in toDestroy)
                {
                    DestroyMeshFilterAndRenderer(gameObject);
                    DestroyImmediate(gameObject);
                }
            }
            
            if(parentOfDynamic)
                return false;
            
            return true;
        }

        private static void DestroyMeshFilterAndRenderer(GameObject go)
        {
            if(go.TryGetComponent(out MeshFilter meshFilter))
                DestroyMeshFilter(meshFilter, false);
            if (go.TryGetComponent(out MeshRenderer meshRenderer))
                DestroyMeshRenderer(meshRenderer);
        }
        
        /// <summary>
        /// Destroy mesh filter avoiding memory leak.
        /// </summary>
        /// <param name="meshFilter">Mesh filter to destroy.</param>
        /// <param name="destroyMesh">Specify whether or not the mesh should also be destroy</param>
        public static void DestroyMeshFilter(MeshFilter meshFilter, bool destroyMesh)
        {
            if (destroyMesh)
            {
                meshFilter.sharedMesh.Clear();
                meshFilter.sharedMesh = null;
            }
            
            if (Application.isPlaying)
            {
                if (destroyMesh) Destroy(meshFilter.sharedMesh);
                Destroy(meshFilter);
            }
            else
            {
                if (destroyMesh) DestroyImmediate(meshFilter.sharedMesh);
                DestroyImmediate(meshFilter);
            }
        }
        
        /// <summary>
        /// Destroy Mesh Renderer.
        /// </summary>
        /// <param name="meshRenderer">Mesh Renderer to destroy</param>
        public static void DestroyMeshRenderer(MeshRenderer meshRenderer)
        {
            meshRenderer.materials = Array.Empty<Material>();
            if (Application.isPlaying)
            {
                Destroy(meshRenderer);
            }
            else
            {
                DestroyImmediate(meshRenderer);
            }
        }
        
        private class CombinedMeshBuilder
        {
            private GameObject _parent;
            private Mesh _mesh;
            private List<MeshFilter> _meshCollector;
            private string _name;
            private Material[] _materials;
            private bool _withRenderer;
            private bool _withCollider;
            
            public CombinedMeshBuilder(GameObject parent, Mesh mesh, List<MeshFilter> meshCollector, string name)
            {
                _parent = parent;
                _mesh = mesh;
                _meshCollector = meshCollector;
                _name = name;
            }
            
            public CombinedMeshBuilder AddRenderer(Material[] materials)
            {
                _withRenderer = true;
                _materials = materials;
                return this;
            }
            
            public CombinedMeshBuilder AddCollider()
            {
                _withCollider = true;
                return this;
            }
            
            public GameObject Build()
            {
                GameObject go = new GameObject(_name);
                go.transform.SetParent(_parent.transform);
                go.transform.localPosition = Vector3.zero;
                go.isStatic = true;
                
                MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = _mesh;
                _meshCollector.Add(meshFilter);
                
                if (_withRenderer)
                {
                    MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterials = _materials;
                }
                
                if (_withCollider)
                {
                    MeshCollider meshCollider = go.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = _mesh;
                }
                
                return go;
            }
        }
    }   
}