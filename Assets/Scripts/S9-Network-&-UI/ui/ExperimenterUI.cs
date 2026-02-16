using Mirror;
using UnityEngine;
using UnityEngine.UI;  // Keep this
using System.Collections.Generic;
using TMPro; // Add if using TextMeshPro

// Add this alias to resolve the conflict:
using Button = UnityEngine.UI.Button;
using Text = UnityEngine.UI.Text;
using Toggle = UnityEngine.UI.Toggle;

public class ExperimenterUI : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainPanel;
    public Text statusText;
    public Text connectionText;

    [Header("Player Info")]
    public Text participantIDText;
    public Text currentSceneText;
    public Text modeText;

    [Header("Controls")]
    public Toggle followCameraToggle;
    public Button resetViewButton;

    private NetworkedPlayer observedPlayer;

    void Start()
    {
        resetViewButton.onClick.AddListener(ResetCameraView);
        InvokeRepeating(nameof(UpdatePlayerInfo), 0f, 0.5f);
    }

    void UpdatePlayerInfo()
    {
        if (observedPlayer == null)
        {
            // Find player
            var players = FindObjectsOfType<NetworkedPlayer>();
            foreach (var player in players)
            {
                if (!player.GetComponent<ExperimenterController>()) // Not an experimenter
                {
                    observedPlayer = player;
                    break;
                }
            }
        }

        if (observedPlayer != null)
        {
            participantIDText.text = $"ID: {observedPlayer.participantID}";
            currentSceneText.text = $"Scene: {observedPlayer.currentScene}";
            modeText.text = observedPlayer.isVRMode ? "Mode: VR" : "Mode: Desktop";
            statusText.text = "Connected";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "Waiting for player...";
            statusText.color = Color.yellow;
        }
    }

    void ResetCameraView()
    {
        var experimenterController = GetComponent<ExperimenterController>();
        if (experimenterController != null)
        {
            experimenterController.transform.position = Vector3.zero + Vector3.up * 2;
            experimenterController.transform.rotation = Quaternion.identity;
        }
    }
}