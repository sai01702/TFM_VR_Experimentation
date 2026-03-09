using Unity.Netcode;
using UnityEngine;

public class NetworkRigSpawner : NetworkBehaviour
{
    public static NetworkRigSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnRigServerRpc(bool isVR, Vector3 pos, Quaternion rot, ServerRpcParams rpcParams = default)
    {
        var installer = FindObjectOfType<SceneRigInstaller>();
        GameObject prefab = isVR ? installer.vrRigPrefab : installer.desktopRigPrefab;

        var rig = Instantiate(prefab, pos, rot);

        var netObj = rig.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
    }
}