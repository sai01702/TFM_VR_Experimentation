using UnityEngine;

/// <summary>
/// Developer-only notes on this GameObject. Edit in the Inspector only; nothing is drawn in Scene or Game view.
/// Persists when you save the scene or prefab (serialized like any other component). Do not read from gameplay code.
/// </summary>
[DisallowMultipleComponent]
public class ObjectNotes : MonoBehaviour
{
    [SerializeField]
    [TextArea(4, 20)]
    [Tooltip("Save the scene or prefab (Ctrl+S) to keep these notes. Not shown at runtime in Scene/Game view.")]
    string body;

#if UNITY_EDITOR
    /// <summary>Editor / tooling access only; avoid using in player code.</summary>
    public string Body
    {
        get => body;
        set => body = value;
    }
#endif
}