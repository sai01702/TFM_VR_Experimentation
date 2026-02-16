using Mirror;
using UnityEngine;

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

    private void Update()
    {
        if (isLocalPlayer)
        {
            // Send local transforms to server
            CmdUpdateTransforms(
                cameraTransform.position,
                cameraTransform.rotation
            );
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

    [Command]
    void CmdUpdateTransforms(Vector3 camPos, Quaternion camRot)
    {
        syncCameraPos = camPos;
        syncCameraRot = camRot;
    }

    [Command]
    public void CmdUpdateSceneInfo(string sceneID, string participantName)
    {
        currentScene = sceneID;
        participantID = participantName;
    }
}