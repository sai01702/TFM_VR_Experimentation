using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class UIKeyboardNavigator : MonoBehaviour
{
    [Tooltip("First element to focus when user presses Tab for the first time.")]
    public GameObject firstSelected;

    [Tooltip("List of selectables to cycle through with Tab. If empty, uses auto-detection.")]
    public List<Selectable> selectables = new List<Selectable>();

    [Tooltip("Enable debug logging")]
    public bool enableDebug = true;

    [Tooltip("Work in all modes, not just Desktop")]
    public bool workInAllModes = true;

    private int currentIndex = -1;
    private bool tabHandledThisFrame = false;

    void LateUpdate()
    {
        // Reset the flag at the end of each frame
        tabHandledThisFrame = false;
    }

    void Start()
    {
        // Auto-find selectables if not assigned
        if (selectables.Count == 0)
        {
            // Find all selectables in the scene
            var allSelectables = FindObjectsByType<Selectable>(FindObjectsSortMode.None);
            foreach (var sel in allSelectables)
            {
                if (sel.interactable && sel.gameObject.activeInHierarchy)
                {
                    selectables.Add(sel);
                }
            }
            
            // Sort by vertical position (top to bottom)
            selectables.Sort((a, b) => {
                RectTransform rtA = a.GetComponent<RectTransform>();
                RectTransform rtB = b.GetComponent<RectTransform>();
                if (rtA == null || rtB == null) return 0;
                return rtB.position.y.CompareTo(rtA.position.y);
            });

            if (enableDebug)
            {
                Debug.Log($"[UIKeyboardNavigator] Auto-found {selectables.Count} selectables:");
                foreach (var sel in selectables)
                {
                    Debug.Log($"  - {sel.gameObject.name}");
                }
            }
        }
    }

    void Update()
    {
        if (!workInAllModes && !IsDesktop()) return;

        var es = EventSystem.current;
        if (es == null) return;

        var kb = Keyboard.current;
        
        // Use new Input System if available
        bool tabPressed = false;
        bool shiftHeld = false;
        bool spacePressed = false;
        bool enterPressed = false;

        if (kb != null)
        {
            tabPressed = kb.tabKey.wasPressedThisFrame;
            shiftHeld = kb.shiftKey.isPressed;
            spacePressed = kb.spaceKey.wasPressedThisFrame;
            enterPressed = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
        }

        // TAB navigation (skip if already handled by OnGUI this frame)
        if (tabPressed && !tabHandledThisFrame)
        {
            tabHandledThisFrame = true;
            if (enableDebug)
                Debug.Log("[UIKeyboardNavigator] Tab pressed (Update)");

            if (selectables.Count > 0)
            {
                // Find current index
                if (es.currentSelectedGameObject != null)
                {
                    for (int i = 0; i < selectables.Count; i++)
                    {
                        if (selectables[i] != null && selectables[i].gameObject == es.currentSelectedGameObject)
                        {
                            currentIndex = i;
                            break;
                        }
                    }
                }

                // Move to next/previous
                if (shiftHeld)
                {
                    currentIndex--;
                    if (currentIndex < 0) currentIndex = selectables.Count - 1;
                }
                else
                {
                    currentIndex++;
                    if (currentIndex >= selectables.Count) currentIndex = 0;
                }

                // Select the new element
                if (currentIndex >= 0 && currentIndex < selectables.Count && selectables[currentIndex] != null)
                {
                    var newSelection = selectables[currentIndex].gameObject;
                    es.SetSelectedGameObject(newSelection);
                    
                    // If it's an input field, activate it
                    var inputField = newSelection.GetComponent<TMPro.TMP_InputField>();
                    if (inputField != null)
                    {
                        inputField.ActivateInputField();
                    }

                    if (enableDebug)
                        Debug.Log($"[UIKeyboardNavigator] Selected: {newSelection.name}");
                }
            }
            else if (firstSelected != null)
            {
                es.SetSelectedGameObject(firstSelected);
                if (enableDebug)
                    Debug.Log($"[UIKeyboardNavigator] Selected firstSelected: {firstSelected.name}");
            }
        }

        // SPACE or ENTER = confirm (click)
        if (spacePressed || enterPressed)
        {
            var current = es.currentSelectedGameObject;
            if (current != null)
            {
                // Don't trigger on input fields with Enter (let them handle it)
                var inputField = current.GetComponent<TMPro.TMP_InputField>();
                if (inputField == null || spacePressed)
                {
                    if (enableDebug)
                        Debug.Log($"[UIKeyboardNavigator] Activating: {current.name}");
                    
                    // Try button click first
                    var button = current.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.Invoke();
                    }
                    else
                    {
                        ExecuteEvents.Execute(current, new BaseEventData(es), ExecuteEvents.submitHandler);
                    }
                }
            }
        }
    }

    // Fallback using OnGUI for Tab detection
    void OnGUI()
    {
        if (!workInAllModes && !IsDesktop()) return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Tab && !tabHandledThisFrame)
        {
            tabHandledThisFrame = true;
            if (enableDebug)
                Debug.Log("[UIKeyboardNavigator] OnGUI Tab detected");

            var es = EventSystem.current;
            if (es == null || selectables.Count == 0) return;

            // Find current index
            if (es.currentSelectedGameObject != null)
            {
                for (int i = 0; i < selectables.Count; i++)
                {
                    if (selectables[i] != null && selectables[i].gameObject == es.currentSelectedGameObject)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Move to next/previous
            if (e.shift)
            {
                currentIndex--;
                if (currentIndex < 0) currentIndex = selectables.Count - 1;
            }
            else
            {
                currentIndex++;
                if (currentIndex >= selectables.Count) currentIndex = 0;
            }

            // Select the new element
            if (currentIndex >= 0 && currentIndex < selectables.Count && selectables[currentIndex] != null)
            {
                var newSelection = selectables[currentIndex].gameObject;
                es.SetSelectedGameObject(newSelection);

                // If it's an input field, activate it
                var inputField = newSelection.GetComponent<TMPro.TMP_InputField>();
                if (inputField != null)
                {
                    inputField.ActivateInputField();
                }

                if (enableDebug)
                    Debug.Log($"[UIKeyboardNavigator] OnGUI Selected: {newSelection.name}");
            }

            e.Use(); // Consume the event
        }
    }

    static bool IsDesktop() =>
        GameSettings.Instance != null && GameSettings.Instance.CurrentMode == GameMode.Desktop;
}

