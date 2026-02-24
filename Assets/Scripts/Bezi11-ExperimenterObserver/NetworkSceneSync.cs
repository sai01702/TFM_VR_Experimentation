using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bezi11.ExperimenterObserver
{
    public class NetworkSceneSync : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (NetworkSessionManager.Instance != null)
            {
                NetworkSessionManager.Instance.UpdateSessionData();
                Debug.Log($"[NetworkSceneSync] Scene changed to: {scene.name}");
            }
        }
    }
}
