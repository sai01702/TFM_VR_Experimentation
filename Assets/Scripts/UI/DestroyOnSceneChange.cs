using UnityEngine;
using UnityEngine.SceneManagement;

public class DestroyOnSceneChange : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("If true, this GameObject will be destroyed when ANY scene loads after the initial scene")]
    [SerializeField] private bool destroyOnAnySceneChange = true;
    
    [Tooltip("Specific scenes to destroy on (only used if destroyOnAnySceneChange is false)")]
    [SerializeField] private string[] scenesToDestroyOn;

    private string initialScene;
    private bool hasSceneChanged = false;

    private void Awake()
    {
        // Remember the scene we started in
        initialScene = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        Debug.Log($"[DestroyOnSceneChange] Initialized on {gameObject.name} in scene {initialScene}");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If this is the first scene load after awake, ignore it (that's our initial scene)
        if (scene.name == initialScene && !hasSceneChanged)
        {
            Debug.Log($"[DestroyOnSceneChange] Ignoring initial scene load: {scene.name}");
            return;
        }

        hasSceneChanged = true;
        bool shouldDestroy = false;

        if (destroyOnAnySceneChange)
        {
            shouldDestroy = true;
            Debug.Log($"[DestroyOnSceneChange] Scene changed from {initialScene} to {scene.name}, will destroy {gameObject.name}");
        }
        else if (scenesToDestroyOn != null && scenesToDestroyOn.Length > 0)
        {
            foreach (string sceneName in scenesToDestroyOn)
            {
                if (scene.name == sceneName)
                {
                    shouldDestroy = true;
                    Debug.Log($"[DestroyOnSceneChange] Scene {scene.name} is in destroy list, will destroy {gameObject.name}");
                    break;
                }
            }
        }

        if (shouldDestroy && gameObject != null)
        {
            Debug.Log($"[DestroyOnSceneChange] Destroying {gameObject.name} because scene changed to {scene.name}");
            Destroy(gameObject);
        }
    }
}
