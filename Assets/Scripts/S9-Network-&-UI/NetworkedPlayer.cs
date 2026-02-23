using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkedPlayer : NetworkBehaviour
{
    [Header("References")]
    public Transform cameraTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;

    [Header("Sync Settings")]
    [SyncVar] public string participantID;
    [SyncVar] public string currentScene;
    [SyncVar] public bool isVRMode;

    // Sync transforms
    [SyncVar] private Vector3 syncCameraPos;
    [SyncVar] private Quaternion syncCameraRot;

    // Read-only accessors for the experimenter controller on the client
    public Vector3 SyncedCameraPos => syncCameraPos;
    public Quaternion SyncedCameraRot => syncCameraRot;

    // Cache last sent values to avoid spamming commands every frame
    private string _lastParticipantID;
    private string _lastSceneName;
    private bool _lastIsVRMode;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // cameraTransform stays null on remote clients.
        // ExperimenterController reads SyncedCameraPos/Rot directly instead.
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            // Host side: rig may be spawned later by SceneRigInstaller.
            // If we don't have a camera yet, try to grab the active main camera.
            if (cameraTransform == null)
            {
                TryAssignCameraFromCurrentRig();
            }

            if (cameraTransform != null)
            {
                // Send local transforms to server
                CmdUpdateTransforms(
                    cameraTransform.position,
                    cameraTransform.rotation
                );
            }

            // Keep participant / scene / mode info in sync for the experimenter UI
            TrySyncSceneInfo();
        }
        else
        {
            // Apply synced transforms on clients
            if (cameraTransform != null)
            {
                cameraTransform.position = Vector3.Lerp(
                    cameraTransform.position,
                    syncCameraPos,
                    Time.deltaTime * 10f
                );
                cameraTransform.rotation = Quaternion.Lerp(
                    cameraTransform.rotation,
                    syncCameraRot,
                    Time.deltaTime * 10f
                );
            }
        }
    }

    void TryAssignCameraFromCurrentRig()
    {
        // Prefer a camera that is NOT owned by the experimenter prefab.
        // ExperimenterObserver/ExperimenterController cameras must not be
        // treated as the player camera, or NetworkedPlayer will lerp them
        // toward syncCameraPos (world origin) and snap the view to the ground.
        foreach (var cam in Camera.allCameras)
        {
            // Skip cameras that belong to an experimenter object
            if (cam.GetComponentInParent<ExperimenterController>() != null)
                continue;

            cameraTransform = cam.transform;
            return;
        }

        // Final fallback – should rarely be reached
        var mainCam = Camera.main;
        if (mainCam != null)
            cameraTransform = mainCam.transform;
    }

    /// <summary>
    /// Called by SceneRigInstaller after the rig is spawned to immediately
    /// bind the rig camera — avoids a 1-frame (or more) gap where syncCameraPos
    /// would be sent as (0,0,0) before TryAssignCameraFromCurrentRig catches up.
    /// </summary>
    public void AssignCamera(Transform cam)
    {
        cameraTransform = cam;
        Debug.Log($"[NetworkedPlayer] Camera assigned explicitly: {cam.name}");
    }

    void TrySyncSceneInfo()
    {
        // Participant ID (from ParticipantSession, if present)
        string participant = ParticipantSession.Instance != null
            ? ParticipantSession.Instance.ParticipantId
            : participantID;

        // Current scene name
        string sceneName = SceneManager.GetActiveScene().name;

        // Mode flag (Desktop vs VR)
        bool vrModeFlag = GameSettings.Instance != null &&
                          GameSettings.Instance.CurrentMode == GameMode.VR;

        // Only send to server if something changed
        if (participant == _lastParticipantID &&
            sceneName == _lastSceneName &&
            vrModeFlag == _lastIsVRMode)
        {
            return;
        }

        _lastParticipantID = participant;
        _lastSceneName = sceneName;
        _lastIsVRMode = vrModeFlag;

        CmdUpdateSceneInfo(sceneName, participant, vrModeFlag);
    }

    [Command]
    void CmdUpdateTransforms(Vector3 camPos, Quaternion camRot)
    {
        syncCameraPos = camPos;
        syncCameraRot = camRot;
    }

    [Command]
    public void CmdUpdateSceneInfo(string sceneID, string participantName, bool vrModeFlag)
    {
        currentScene = sceneID;
        participantID = participantName;
        isVRMode = vrModeFlag;
    }
}