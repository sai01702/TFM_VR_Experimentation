using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIKeyboardNavigator : MonoBehaviour
{
    [Tooltip("First element to focus when user presses Tab for the first time.")]
    public GameObject firstSelected;

    void Update()
    {
        if (!IsDesktop()) return;

        var es = EventSystem.current;
        if (es == null) return;

        // --- Mouse handoff: release selection so mouse clicks work normally
        var mouse = Mouse.current;
        if (mouse != null)
        {
            bool mouseMoved = mouse.delta.ReadValue() != Vector2.zero;
            bool mouseClicked = mouse.leftButton.wasPressedThisFrame
                                || mouse.rightButton.wasPressedThisFrame
                                || mouse.middleButton.wasPressedThisFrame;

            if (mouseMoved || mouseClicked)
            {
                if (es.currentSelectedGameObject != null)
                    es.SetSelectedGameObject(null);
            }
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        // --- Prevent WASD and Arrow keys from affecting UI selection
        if (kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ||
            kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ||
            kb.upArrowKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame ||
            kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
        {
            if (es.currentSelectedGameObject != null)
                es.SetSelectedGameObject(null);
        }

        // --- TAB / SHIFT+TAB to navigate between buttons
        if (kb.tabKey.wasPressedThisFrame)
        {
            if (es.currentSelectedGameObject == null && firstSelected != null)
            {
                es.SetSelectedGameObject(firstSelected);
            }
            else
            {
                var current = es.currentSelectedGameObject;
                if (current != null)
                {
                    var sel = current.GetComponent<Selectable>();
                    if (sel != null)
                    {
                        bool backwards = kb.shiftKey.isPressed;
                        Selectable next = backwards
                            ? (sel.FindSelectableOnUp() ?? sel.FindSelectableOnLeft())
                            : (sel.FindSelectableOnDown() ?? sel.FindSelectableOnRight());

                        if (next == null && firstSelected != null)
                            next = firstSelected.GetComponent<Selectable>();

                        if (next != null)
                            es.SetSelectedGameObject(next.gameObject);
                    }
                }
                else if (firstSelected != null)
                {
                    es.SetSelectedGameObject(firstSelected);
                }
            }
        }

        // --- SPACE = confirm (click)
        if (kb.spaceKey.wasPressedThisFrame)
        {
            var current = es.currentSelectedGameObject;
            if (current != null)
                ExecuteEvents.Execute(current, new BaseEventData(es), ExecuteEvents.submitHandler);
        }
    }

    static bool IsDesktop() =>
        GameSettings.Instance != null && GameSettings.Instance.CurrentMode == GameMode.Desktop;
}