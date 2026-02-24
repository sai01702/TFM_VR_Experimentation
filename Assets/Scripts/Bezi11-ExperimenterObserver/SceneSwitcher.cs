using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bezi11.ExperimenterObserver
{
    public class SceneSwitcher : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] private string targetSceneName = "ExperimenterClientScene";

        [Header("Optional UI Reference")]
        [SerializeField] private Button switchButton;

        private void Start()
        {
            if (switchButton != null)
            {
                switchButton.onClick.AddListener(SwitchToExperimenterScene);
            }
        }

        public void SwitchToExperimenterScene()
        {
            Debug.Log($"[SceneSwitcher] Loading scene: {targetSceneName}");
            
            // Explicitly destroy SessionRoot before loading new scene
            var sessionRoot = GameObject.Find("SessionRoot");
            if (sessionRoot != null)
            {
                Debug.Log("[SceneSwitcher] Destroying SessionRoot before scene load");
                Destroy(sessionRoot);
            }
            
            SceneManager.LoadScene(targetSceneName);
        }

        private void OnDestroy()
        {
            if (switchButton != null)
            {
                switchButton.onClick.RemoveListener(SwitchToExperimenterScene);
            }
        }
    }
}
