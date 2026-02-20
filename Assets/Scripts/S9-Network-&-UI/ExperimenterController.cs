using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ExperimenterController : NetworkBehaviour
{
    [Header("Camera Settings")]
    public Camera experimenterCamera;
    // Observer now only mirrors the participant's view, so
    // free-fly movement is disabled and we always follow.
    [Header("Follow Settings")]
    public bool isFollowingPlayer = true;

    [Header("UI")]
    public GameObject experimenterUI;
    public Text participantIDText;
    public Text currentSceneText;
    public Toggle followToggle;

    private NetworkedPlayer targetPlayer;
    private float rotationX = 0f;

    void Start()
    {
        if (!isLocalPlayer)
        {
            // Disable camera for non-local experimenters
            if (experimenterCamera != null)
                experimenterCamera.enabled = false;
            if (experimenterUI != null)
                experimenterUI.SetActive(false);
            return;
        }

        // Setup UI
        if (experimenterUI != null)
            experimenterUI.SetActive(true);

        // Force follow mode and disable any manual toggle
        isFollowingPlayer = true;
        if (followToggle != null)
        {
            followToggle.isOn = true;
            followToggle.interactable = false;
        }

        // Find player to observe
        FindTargetPlayer();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (targetPlayer == null)
        {
            // Try to acquire target if it wasn't found at Start()
            FindTargetPlayer();
        }

        UpdateUI();

        // Always follow the participant's camera (screen-share style)
        if (isFollowingPlayer && targetPlayer != null)
        {
            FollowPlayer();
        }
    }

    void FollowPlayer()
    {
        if (targetPlayer == null || targetPlayer.cameraTransform == null) return;

        // Mirror the participant's camera view (screen-share style)
        if (experimenterCamera != null)
        {
            experimenterCamera.transform.position = targetPlayer.cameraTransform.position;
            experimenterCamera.transform.rotation = targetPlayer.cameraTransform.rotation;
        }
        else
        {
            // Fallback: move this GameObject like the participant's camera
            transform.position = targetPlayer.cameraTransform.position;
            transform.rotation = targetPlayer.cameraTransform.rotation;
        }
    }

    void FindTargetPlayer()
    {
        // Find the host player (connectionId 0)
        var players = FindObjectsOfType<NetworkedPlayer>();
        foreach (var player in players)
        {
            if (player.isLocalPlayer && player.netId != this.netId)
            {
                targetPlayer = player;
                Debug.Log("Found target player to observe");
                break;
            }
        }
    }

    void UpdateUI()
    {
        if (targetPlayer != null)
        {
            participantIDText.text = $"Participant: {targetPlayer.participantID}";
            currentSceneText.text = $"Scene: {targetPlayer.currentScene}";
        }
    }

    void OnFollowToggleChanged(bool value)
    {
        // Follow mode is always enforced; ignore external changes.
        isFollowingPlayer = true;
        if (followToggle != null && !followToggle.isOn)
            followToggle.isOn = true;
    }
}