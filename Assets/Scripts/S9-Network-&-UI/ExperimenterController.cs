using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ExperimenterController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Leave empty to auto-use Camera.main in the ExperimenterView scene.")]
    public Camera experimenterCamera;

    [Header("Follow Settings")]
    public bool isFollowingPlayer = true;

    [Header("UI")]
    public GameObject experimenterUI;
    public Text participantIDText;
    public Text currentSceneText;
    public Toggle followToggle;

    private NetworkedPlayer targetPlayer;

    void Start()
    {
        if (!isLocalPlayer)
        {
            if (experimenterCamera != null)
                experimenterCamera.enabled = false;
            if (experimenterUI != null)
                experimenterUI.SetActive(false);
            return;
        }

        // Auto-grab the scene's main camera if not wired up in the Inspector
        if (experimenterCamera == null)
            experimenterCamera = Camera.main;

        if (experimenterUI != null)
            experimenterUI.SetActive(true);

        isFollowingPlayer = true;
        if (followToggle != null)
        {
            followToggle.isOn = true;
            followToggle.interactable = false;
        }

        FindTargetPlayer();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        if (targetPlayer == null)
            FindTargetPlayer();

        UpdateUI();

        if (isFollowingPlayer && targetPlayer != null)
            FollowPlayer();
    }

    void FollowPlayer()
    {
        if (targetPlayer == null || targetPlayer.cameraTransform == null) return;

        Camera cam = experimenterCamera != null ? experimenterCamera : Camera.main;
        if (cam != null)
        {
            cam.transform.position = targetPlayer.cameraTransform.position;
            cam.transform.rotation = targetPlayer.cameraTransform.rotation;
        }
    }

    void FindTargetPlayer()
    {
        // On the experimenter CLIENT the participant's NetworkedPlayer is a
        // REMOTE (non-local) object. Pick the first NetworkedPlayer that is
        // NOT the experimenter itself (no ExperimenterController sibling).
        var players = FindObjectsOfType<NetworkedPlayer>();
        foreach (var player in players)
        {
            if (player.GetComponent<ExperimenterController>() != null)
                continue;   // skip the experimenter's own networked object

            targetPlayer = player;
            Debug.Log($"[ExperimenterController] Observing participant netId={player.netId}");
            return;
        }
    }

    void UpdateUI()
    {
        if (targetPlayer == null) return;

        if (participantIDText != null)
            participantIDText.text = $"Participant: {targetPlayer.participantID}";
        if (currentSceneText != null)
            currentSceneText.text = $"Scene: {targetPlayer.currentScene}";
    }

    void OnFollowToggleChanged(bool value)
    {
        isFollowingPlayer = true;
        if (followToggle != null && !followToggle.isOn)
            followToggle.isOn = true;
    }
}