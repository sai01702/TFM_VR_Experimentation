using UnityEngine;
using TMPro;

namespace Bezi11.ExperimenterObserver
{
    public class ObserverSessionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI participantIdText;
        [SerializeField] private TextMeshProUGUI sceneNameText;
        [SerializeField] private TextMeshProUGUI gameModeText;

        void Start()
        {
            UpdateUI("N/A", "N/A", "N/A");
        }

        void Update()
        {
            if (NetworkSessionManager.Instance != null)
            {
                UpdateUI(
                    NetworkSessionManager.Instance.ParticipantId,
                    NetworkSessionManager.Instance.CurrentScene,
                    NetworkSessionManager.Instance.GameMode
                );
            }
        }

        private void UpdateUI(string participantId, string sceneName, string gameMode)
        {
            if (participantIdText != null)
            {
                participantIdText.text = participantId;
            }

            if (sceneNameText != null)
            {
                sceneNameText.text = sceneName;
            }

            if (gameModeText != null)
            {
                gameModeText.text = gameMode;
            }
        }
    }
}
