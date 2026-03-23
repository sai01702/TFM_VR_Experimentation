using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MazeGen
{
    [ExecuteAlways]
    public class MazeLoader : MonoBehaviour
    {
        public GameObject rootMazeObject;
        public Maze maze;
        public MazeInstantiator mazeInstantiator;
        private bool _loadedWhenPlaying;

        [SerializeField] public List<MeshFilter> bakedMeshes = new List<MeshFilter>();

        private void Awake()
        {
            if (rootMazeObject == null)
            {
                rootMazeObject = GameObject.Find("rootMazeObject");
                if (rootMazeObject == null)
                {
                    rootMazeObject = new GameObject("rootMazeObject");
                }
            }
        }

        public void Create()
        {
            if (maze == null)
            {
                Debug.LogWarning("No maze associate !");
                return;
            }
            
            maze.Create();
        }
        public void Load(bool bakeImmediate,bool autoCreate)
        {
            if (maze == null)
            {
                Debug.LogWarning("No maze associate !");
                return;
            }
            
            if (mazeInstantiator == null)
            {
                Debug.LogWarning("No maze instantiator associate !");
                return;
            }

            if (autoCreate)
            {
                maze.Create();
                IEnumerator WaitToLoad()
                {
                    yield return new WaitWhile(() => !maze.mazeGenerator.Finish);
                    mazeInstantiator.InstantiateMaze(maze.GetMaze(), maze.mazeGenerator.GetRegex(), rootMazeObject.transform, bakeImmediate, bakedMeshes);
                }

                StartCoroutine(WaitToLoad());
            }
            else
            {
                mazeInstantiator.InstantiateMaze(maze.GetMaze(), maze.mazeGenerator.GetRegex(), rootMazeObject.transform, bakeImmediate, bakedMeshes);
            }
            
            _loadedWhenPlaying = Application.isPlaying;
            
            if(!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public void Bake()
        {
            if (mazeInstantiator == null)
            {
                Debug.LogWarning("No maze instantiator associate !");
                return;
            }

            if (_loadedWhenPlaying != Application.isPlaying)
            {
                Debug.LogWarning("Can't bake maze when not loaded at same runtime !");
                return;
            }

            DestroyBakedMeshes();
            mazeInstantiator.BakeAll(rootMazeObject, bakedMeshes);
            
            if(!Application.isPlaying)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        public void Destroy()
        {
            DestroyBakedMeshes();
            GameObject[] toDestroy = new GameObject[rootMazeObject.transform.childCount];
            for (int i = 0; i < rootMazeObject.transform.childCount; i++)
            {
                toDestroy[i] = rootMazeObject.transform.GetChild(i).gameObject;
            }

            if (Application.isPlaying)
            {
                foreach (GameObject o in toDestroy)
                {
                    if(o.TryGetComponent(out MeshFilter meshFilter))
                        MazeInstantiator.DestroyMeshFilter(meshFilter, false);
                    if (o.TryGetComponent(out MeshRenderer meshRenderer))
                        MazeInstantiator.DestroyMeshRenderer(meshRenderer);
                    Destroy(o);
                }
            }
            else
            {
                foreach (GameObject o in toDestroy)
                {
                    if (o.TryGetComponent(out MeshFilter meshFilter))
                        MazeInstantiator.DestroyMeshFilter(meshFilter, false);
                    if (o.TryGetComponent(out MeshRenderer meshRenderer))
                        MazeInstantiator.DestroyMeshRenderer(meshRenderer);
                    DestroyImmediate(o);
                }
            }
        }
        
        public void DestroyBakedMeshes()
        {
            foreach (MeshFilter meshFilter in bakedMeshes)
            {
                if(meshFilter == null) continue;
                if (Application.isPlaying)
                {
                    Destroy(meshFilter.sharedMesh);    
                }
                else
                {
                    DestroyImmediate(meshFilter.sharedMesh);
                }
            }
            bakedMeshes.Clear();
        }
    }
}