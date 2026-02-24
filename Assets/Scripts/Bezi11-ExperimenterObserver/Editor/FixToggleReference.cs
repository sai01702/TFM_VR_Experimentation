using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Bezi11.ExperimenterObserver.Editor
{
    public class FixToggleReference : EditorWindow
    {
        [MenuItem("Tools/Bezi11/Fix Connection Mode Toggle Reference")]
        public static void ShowWindow()
        {
            var window = GetWindow<FixToggleReference>("Fix Toggle Reference");
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Connection Mode Toggle Reference Fix", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This tool will fix the ConnectionModeToggle reference in RoomScene.\n\n" +
                "Current issue: The toggle reference is NULL, preventing mode switching.\n\n" +
                "Click the button below to fix it.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Fix Toggle Reference in RoomScene", GUILayout.Height(40)))
            {
                FixToggleReferenceInScene();
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "After clicking, the scene will be opened and the reference will be assigned.\n" +
                "The scene will be saved automatically.",
                MessageType.Warning
            );
        }

        private static void FixToggleReferenceInScene()
        {
            Debug.Log("[FixToggleReference] Starting fix...");

            // Open RoomScene
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/RoomScene.unity", OpenSceneMode.Single);
            Debug.Log($"[FixToggleReference] Opened scene: {scene.name}");

            // Find ConnectionCodeCanvas
            var canvas = GameObject.Find("ConnectionCodeCanvas");
            if (canvas == null)
            {
                Debug.LogError("[FixToggleReference] ConnectionCodeCanvas not found!");
                EditorUtility.DisplayDialog("Error", "ConnectionCodeCanvas GameObject not found in RoomScene!", "OK");
                return;
            }

            Debug.Log("[FixToggleReference] Found ConnectionCodeCanvas");

            // Get ConnectionCodeGenerator component
            var generator = canvas.GetComponent<ConnectionCodeGenerator>();
            if (generator == null)
            {
                Debug.LogError("[FixToggleReference] ConnectionCodeGenerator component not found!");
                EditorUtility.DisplayDialog("Error", "ConnectionCodeGenerator component not found on ConnectionCodeCanvas!", "OK");
                return;
            }

            Debug.Log("[FixToggleReference] Found ConnectionCodeGenerator component");

            // Find the toggle
            var togglePath = "ConnectionCodeCanvas/ConnectionPanel/ConnectionModeToggle";
            var toggleObj = GameObject.Find(togglePath);
            if (toggleObj == null)
            {
                Debug.LogError($"[FixToggleReference] Toggle not found at path: {togglePath}");
                EditorUtility.DisplayDialog("Error", $"ConnectionModeToggle not found at path: {togglePath}", "OK");
                return;
            }

            Debug.Log($"[FixToggleReference] Found toggle: {toggleObj.name}");

            // Get Toggle component
            var toggle = toggleObj.GetComponent<Toggle>();
            if (toggle == null)
            {
                Debug.LogError("[FixToggleReference] Toggle component not found on GameObject!");
                EditorUtility.DisplayDialog("Error", "Toggle component not found on ConnectionModeToggle GameObject!", "OK");
                return;
            }

            Debug.Log("[FixToggleReference] Found Toggle component");

            // Assign the reference using SerializedObject (proper way for Editor)
            var serializedObject = new SerializedObject(generator);
            var toggleProperty = serializedObject.FindProperty("connectionModeToggle");

            if (toggleProperty == null)
            {
                Debug.LogError("[FixToggleReference] connectionModeToggle property not found!");
                EditorUtility.DisplayDialog("Error", "connectionModeToggle property not found in ConnectionCodeGenerator!", "OK");
                return;
            }

            // Assign the toggle
            toggleProperty.objectReferenceValue = toggle;
            serializedObject.ApplyModifiedProperties();

            Debug.Log("[FixToggleReference] Toggle reference assigned!");

            // Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[FixToggleReference] Scene saved!");

            // Show success message
            EditorUtility.DisplayDialog(
                "Success!",
                "Toggle reference has been fixed and saved!\n\n" +
                "The connectionModeToggle is now properly assigned.\n\n" +
                "You can now test the toggle in Play Mode.",
                "OK"
            );

            Debug.Log("[FixToggleReference] Fix complete!");
        }
    }
}
