using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

#if UNITY_SERVICES_INSTALLED
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif

namespace Bezi11.ExperimenterObserver
{
    public class RelayConnectionManager : MonoBehaviour
    {
        public static RelayConnectionManager Instance { get; private set; }

        [Header("Connection Mode")]
        [SerializeField] private bool useRelayForInternetConnection = true;

        public bool IsUsingRelay => useRelayForInternetConnection;
        public string JoinCode { get; private set; }

        private const int MaxConnections = 5;
        private bool isInitializing = false;

        public void SetConnectionMode(bool useRelay)
        {
            useRelayForInternetConnection = useRelay;
            Debug.Log($"[RelayConnectionManager] Connection mode set to: {(useRelay ? "Relay" : "LAN")}");
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
#if UNITY_SERVICES_INSTALLED
            if (useRelayForInternetConnection)
            {
                await InitializeUnityServices();
            }
#else
            if (useRelayForInternetConnection)
            {
                Debug.LogWarning("[RelayConnectionManager] Unity Services packages not installed. Relay mode disabled. Install packages to enable internet connectivity.");
                useRelayForInternetConnection = false;
            }
#endif
        }

        private async Task InitializeUnityServices()
        {
#if UNITY_SERVICES_INSTALLED
            if (isInitializing)
            {
                Debug.Log("[RelayConnectionManager] Already initializing, waiting...");
                while (isInitializing)
                {
                    await Task.Delay(100);
                }
                return;
            }

            try
            {
                isInitializing = true;

                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                    Debug.Log("[RelayConnectionManager] Unity Services initialized");
                }

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log("[RelayConnectionManager] Signed in anonymously");
                }
                else
                {
                    Debug.Log("[RelayConnectionManager] Already signed in");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayConnectionManager] Failed to initialize Unity Services: {e.Message}");
            }
            finally
            {
                isInitializing = false;
            }
#else
            await Task.CompletedTask;
#endif
        }

        public async Task<string> StartHostWithRelay()
        {
#if UNITY_SERVICES_INSTALLED
            if (!useRelayForInternetConnection)
            {
                Debug.LogWarning("[RelayConnectionManager] Relay is disabled. Use LAN mode instead.");
                return null;
            }

            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await InitializeUnityServices();
                }

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
                Debug.Log($"[RelayConnectionManager] Relay allocation created");

                JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"[RelayConnectionManager] Join Code: {JoinCode}");

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetHostRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                if (ParticipantSession.Instance != null)
                {
                    ParticipantSession.Instance.AppendLog($"[Network] Relay Join Code: {JoinCode}");
                }

                return JoinCode;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayConnectionManager] Failed to start relay host: {e.Message}");
                return null;
            }
#else
            Debug.LogError("[RelayConnectionManager] Unity Services packages not installed. Cannot use relay.");
            await Task.CompletedTask;
            return null;
#endif
        }

        public async Task<bool> JoinWithRelay(string joinCode)
        {
#if UNITY_SERVICES_INSTALLED
            if (!useRelayForInternetConnection)
            {
                Debug.LogWarning("[RelayConnectionManager] Relay is disabled. Use LAN mode instead.");
                return false;
            }

            try
            {
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await InitializeUnityServices();
                }

                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log($"[RelayConnectionManager] Joined relay with code: {joinCode}");

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetClientRelayData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData,
                    allocation.HostConnectionData
                );

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayConnectionManager] Failed to join relay: {e.Message}");
                return false;
            }
#else
            Debug.LogError("[RelayConnectionManager] Unity Services packages not installed. Cannot use relay.");
            await Task.CompletedTask;
            return false;
#endif
        }
    }
}
