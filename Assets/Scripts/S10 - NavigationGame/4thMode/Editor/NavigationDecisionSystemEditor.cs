using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationDecisionSystem))]
public class NavigationDecisionSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NavigationDecisionSystem system = (NavigationDecisionSystem)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("V4 Visual Controls", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("All Visuals On"))
            SetAll(system, true);

        if (GUILayout.Button("All Visuals Off"))
            SetAll(system, false);
        EditorGUILayout.EndHorizontal();

        DrawToggleButtons(system, "Runtime Root", nameof(system.showRuntimeVisuals));
        DrawToggleButtons(system, "Ideal Path Lines", nameof(system.showIdealPathLines));
        DrawToggleButtons(system, "Start / Goal", nameof(system.showStartGoalMarkers));
        DrawToggleButtons(system, "Decision Nodes", nameof(system.showDecisionNodes));
        DrawToggleButtons(system, "Path Beginning Points", nameof(system.showPathBeginningPoints));
        DrawToggleButtons(system, "Off Route Nodes", nameof(system.showOffRouteNodes));
        DrawToggleButtons(system, "Correct Route Triangles", nameof(system.showRouteAnchors));
        DrawToggleButtons(system, "Branch Triangles", nameof(system.showBranchAnchors));
        DrawToggleButtons(system, "Walkable Area Triangles", nameof(system.showWalkableAreaAnchors));
        DrawToggleButtons(system, "Corridor Triangles", nameof(system.showCorridorAnchors));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Runtime Visuals"))
            Refresh(system);

        if (GUILayout.Button("Clear Runtime Visuals"))
            Clear(system);
        EditorGUILayout.EndHorizontal();
    }

    void DrawToggleButtons(NavigationDecisionSystem system, string label, string propertyName)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);

        if (property == null || property.propertyType != SerializedPropertyType.Boolean)
            return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(170f));

        if (GUILayout.Button("On"))
            SetBool(system, property, true);

        if (GUILayout.Button("Off"))
            SetBool(system, property, false);

        EditorGUILayout.EndHorizontal();
    }

    void SetAll(NavigationDecisionSystem system, bool value)
    {
        Undo.RecordObject(system, value ? "Turn On V4 Visuals" : "Turn Off V4 Visuals");
        system.SetAllRuntimeVisuals(value);
        EditorUtility.SetDirty(system);
        RefreshOrClear(system);
    }

    void SetBool(NavigationDecisionSystem system, SerializedProperty property, bool value)
    {
        serializedObject.Update();
        property.boolValue = value;

        if (value && property.name != nameof(system.showRuntimeVisuals))
            serializedObject.FindProperty(nameof(system.showRuntimeVisuals)).boolValue = true;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(system);
        RefreshOrClear(system);
    }

    void RefreshOrClear(NavigationDecisionSystem system)
    {
        if (!Application.isPlaying)
            return;

        if (system.showRuntimeVisuals)
            system.RefreshRuntimeVisuals();
        else
            system.ClearAllRuntimeVisuals();
    }

    void Refresh(NavigationDecisionSystem system)
    {
        if (!Application.isPlaying)
            return;

        system.RefreshRuntimeVisuals();
    }

    void Clear(NavigationDecisionSystem system)
    {
        if (!Application.isPlaying)
            return;

        system.ClearAllRuntimeVisuals();
    }
}
