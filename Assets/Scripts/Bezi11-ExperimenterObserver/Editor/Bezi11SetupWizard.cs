using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Bezi11.ExperimenterObserver.Editor
{
    public class Bezi11SetupWizard : EditorWindow
    {
        private Vector2 scrollPosition;

        [MenuItem("Tools/Bezi11/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<Bezi11SetupWizard>("Bezi11 Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Bezi11 Experimenter Observer - Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This wizard helps you set up the Experimenter Observer System. " +
                "Follow the steps in order.",
                MessageType.Info);

            GUILayout.Space(20);

            DrawPackageCheck();
            GUILayout.Space(10);

            DrawNetworkManagerSetup();
            GUILayout.Space(10);

            DrawRigSetup();
            GUILayout.Space(10);

            DrawSceneSetup();
            GUILayout.Space(10);

            DrawDocumentation();

            EditorGUILayout.EndScrollView();
        }

        void DrawPackageCheck()
        {
            EditorGUILayout.LabelField("Step 1: Package Installation", EditorStyles.boldLabel);

            bool hasNetcode = System.Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime") != null;

            if (hasNetcode)
            {
                EditorGUILayout.HelpBox("✓ Unity Netcode for GameObjects is installed!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Unity Netcode for GameObjects is NOT installed!\n\n" +
                    "Install via: Window > Package Manager > Add package by name > com.unity.netcode.gameobjects",
                    MessageType.Error);
            }
        }

        void DrawNetworkManagerSetup()
        {
            EditorGUILayout.LabelField("Step 2: NetworkManager Setup", EditorStyles.boldLabel);

            string prefabPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkManager.prefab";
            string networkPrefabsListPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/Bezi11NetworkPrefabs.asset";
            
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var networkPrefabsList = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>(networkPrefabsListPath);

            if (existingPrefab != null && networkPrefabsList != null)
            {
                EditorGUILayout.HelpBox("✓ NetworkManager prefab exists!\n✓ Network Prefabs List configured!", MessageType.Info);
                
                if (GUILayout.Button("Select NetworkManager Prefab"))
                {
                    Selection.activeObject = existingPrefab;
                    EditorGUIUtility.PingObject(existingPrefab);
                }

                GUILayout.Space(5);

                EditorGUILayout.HelpBox(
                    "Want to recreate? Delete the existing prefabs first, then click Create button.",
                    MessageType.None);

                if (GUILayout.Button("Delete NetworkManager & Recreate", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Confirm Delete",
                        "This will delete:\n• NetworkManager.prefab\n• NetworkSessionManager.prefab\n• Bezi11NetworkPrefabs.asset\n\nYou can recreate them immediately after. Continue?",
                        "Yes, Delete",
                        "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(prefabPath);
                        AssetDatabase.DeleteAsset("Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkSessionManager.prefab");
                        AssetDatabase.DeleteAsset(networkPrefabsListPath);
                        AssetDatabase.Refresh();
                        
                        EditorUtility.DisplayDialog("Deleted", "Prefabs deleted. Click 'Create NetworkManager GameObject' to recreate.", "OK");
                    }
                }
            }
            else if (existingPrefab != null && networkPrefabsList == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ NetworkManager exists but Network Prefabs List is missing!\n\nClick the button below to create and configure it.",
                    MessageType.Warning);

                if (GUILayout.Button("Create Missing Network Prefabs List", GUILayout.Height(30)))
                {
                    CreateNetworkPrefabsListOnly();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "NetworkManager prefab not found. Click the button below to create it.\n\n" +
                    "This will automatically create:\n" +
                    "• NetworkManager.prefab\n" +
                    "• NetworkSessionManager.prefab\n" +
                    "• Bezi11NetworkPrefabs.asset (pre-configured!)",
                    MessageType.Warning);

                if (GUILayout.Button("Create NetworkManager GameObject", GUILayout.Height(30)))
                {
                    CreateNetworkManagerPrefab();
                }
            }

            GUILayout.Space(5);

            string sessionManagerPath = "Assets/Prefabs/Bezi11-ExperimenterObserver/NetworkSessionManager.prefab";
            GameObject sessionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sessionManagerPath);

            if (sessionPrefab != null)
            {
                EditorGUILayout.HelpBox("✓ NetworkSessionManager prefab exists!", MessageType.Info);
            }
        }

        void DrawRigSetup()
        {
            EditorGUILayout.LabelField("Step 3: Rig Setup", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "You need to manually add the PlayerCameraStreamer component to your rig prefabs:\n\n" +
                "• Desktop Rig: /Assets/Prefabs/S1 - Rigs/Desktop Rig.prefab\n" +
                "• VR Rigs: Find in /Assets/Prefabs/S1 - Rigs/\n\n" +
                "See the Manual Setup Checklist page for detailed instructions.",
                MessageType.Info);

            if (GUILayout.Button("Open Manual Setup Checklist"))
            {
                string checklistPath = "Assets/Pages/Bezi11 Implementation Checklist.md";
                var checklist = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(checklistPath);
                if (checklist != null)
                {
                    Selection.activeObject = checklist;
                }
            }
        }

        void DrawSceneSetup()
        {
            EditorGUILayout.LabelField("Step 4: Scene Setup", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Manual scene setup required:\n\n" +
                "1. Add NetworkBootstrap to 0-StartMenu scene\n" +
                "2. Add ConnectionCodeCanvas to RoomScene\n" +
                "3. Create ExperimenterClientScene with UI\n\n" +
                "See the Manual Setup Checklist for detailed instructions.",
                MessageType.Info);
        }

        void DrawDocumentation()
        {
            EditorGUILayout.LabelField("Documentation & Guides", EditorStyles.boldLabel);

            if (GUILayout.Button("Open Experimenter Setup Guide"))
            {
                string guidePath = "Assets/Pages/Experimenter Setup Guide.md";
                var guide = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(guidePath);
                if (guide != null)
                {
                    Selection.activeObject = guide;
                }
            }

            if (GUILayout.Button("Open Implementation Checklist"))
            {
                string checklistPath = "Assets/Pages/Bezi11 Implementation Checklist.md";
                var checklist = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(checklistPath);
                if (checklist != null)
                {
                    Selection.activeObject = checklist;
                }
            }

            if (GUILayout.Button("Open Main Plan"))
            {
                string planPath = "Plans/Experimenter Observer System.md";
                var plan = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(planPath);
                if (plan != null)
                {
                    Selection.activeObject = plan;
                }
            }
        }

        void CreateNetworkManagerPrefab()
        {
            string folderPath = "Assets/Prefabs/Bezi11-ExperimenterObserver";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = "Assets/Prefabs";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder(parentFolder, "Bezi11-ExperimenterObserver");
            }

            GameObject sessionManagerGO = new GameObject("NetworkSessionManager");
            sessionManagerGO.AddComponent<NetworkObject>();
            sessionManagerGO.AddComponent<NetworkSessionManager>();
            sessionManagerGO.AddComponent<NetworkSceneSync>();
            sessionManagerGO.AddComponent<NetworkDisconnectHandler>();

            string sessionPrefabPath = folderPath + "/NetworkSessionManager.prefab";
            GameObject sessionPrefab = PrefabUtility.SaveAsPrefabAsset(sessionManagerGO, sessionPrefabPath);
            DestroyImmediate(sessionManagerGO);

            var networkPrefabsList = ScriptableObject.CreateInstance<Unity.Netcode.NetworkPrefabsList>();
            networkPrefabsList.Add(new Unity.Netcode.NetworkPrefab { Prefab = sessionPrefab });
            
            string networkPrefabsListPath = folderPath + "/Bezi11NetworkPrefabs.asset";
            AssetDatabase.CreateAsset(networkPrefabsList, networkPrefabsListPath);

            GameObject networkManagerGO = new GameObject("NetworkManager");
            var networkManager = networkManagerGO.AddComponent<NetworkManager>();
            var unityTransport = networkManagerGO.AddComponent<UnityTransport>();
            
            unityTransport.ConnectionData.Port = 7777;

            var networkConfig = networkManager.NetworkConfig;
            var prefabsLists = new System.Collections.Generic.List<Unity.Netcode.NetworkPrefabsList>();
            prefabsLists.Add(networkPrefabsList);
            networkConfig.Prefabs.NetworkPrefabsLists = prefabsLists;
            networkManager.NetworkConfig = networkConfig;

            string prefabPath = folderPath + "/NetworkManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(networkManagerGO, prefabPath);
            DestroyImmediate(networkManagerGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Success!",
                "✅ NetworkManager setup complete!\n\n" +
                "Created:\n" +
                "• NetworkManager.prefab\n" +
                "• NetworkSessionManager.prefab\n" +
                "• Bezi11NetworkPrefabs.asset (auto-configured!)\n\n" +
                "Location: " + folderPath + "\n\n" +
                "Next Step:\n" +
                "Add NetworkBootstrap to 0-StartMenu scene and assign the NetworkManager prefab.",
                "OK");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        void CreateNetworkPrefabsListOnly()
        {
            string folderPath = "Assets/Prefabs/Bezi11-ExperimenterObserver";
            string sessionPrefabPath = folderPath + "/NetworkSessionManager.prefab";
            GameObject sessionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sessionPrefabPath);

            if (sessionPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "NetworkSessionManager.prefab not found! Please create it first.", "OK");
                return;
            }

            var networkPrefabsList = ScriptableObject.CreateInstance<Unity.Netcode.NetworkPrefabsList>();
            networkPrefabsList.Add(new Unity.Netcode.NetworkPrefab { Prefab = sessionPrefab });
            
            string networkPrefabsListPath = folderPath + "/Bezi11NetworkPrefabs.asset";
            AssetDatabase.CreateAsset(networkPrefabsList, networkPrefabsListPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string prefabPath = folderPath + "/NetworkManager.prefab";
            GameObject networkManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (networkManagerPrefab != null)
            {
                using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
                {
                    var root = editingScope.prefabContentsRoot;
                    var networkManager = root.GetComponent<Unity.Netcode.NetworkManager>();
                    
                    if (networkManager != null)
                    {
                        var networkConfig = networkManager.NetworkConfig;
                        var prefabsLists = new System.Collections.Generic.List<Unity.Netcode.NetworkPrefabsList>();
                        prefabsLists.Add(networkPrefabsList);
                        networkConfig.Prefabs.NetworkPrefabsLists = prefabsLists;
                        networkManager.NetworkConfig = networkConfig;
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Success!",
                "✅ Network Prefabs List created and configured!\n\n" +
                "Created: Bezi11NetworkPrefabs.asset\n" +
                "Added: NetworkSessionManager.prefab to the list\n" +
                "Linked: To NetworkManager.prefab",
                "OK");

            Selection.activeObject = networkPrefabsList;
        }

        void CreateNetworkSessionManagerPrefab()
        {
            string folderPath = "Assets/Prefabs/Bezi11-ExperimenterObserver";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = "Assets/Prefabs";
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder(parentFolder, "Bezi11-ExperimenterObserver");
            }

            GameObject sessionManagerGO = new GameObject("NetworkSessionManager");
            sessionManagerGO.AddComponent<NetworkObject>();
            sessionManagerGO.AddComponent<NetworkSessionManager>();
            sessionManagerGO.AddComponent<NetworkSceneSync>();
            sessionManagerGO.AddComponent<NetworkDisconnectHandler>();

            string prefabPath = folderPath + "/NetworkSessionManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(sessionManagerGO, prefabPath);
            DestroyImmediate(sessionManagerGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success", "NetworkSessionManager prefab created at:\n" + prefabPath, "OK");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
    }
}
