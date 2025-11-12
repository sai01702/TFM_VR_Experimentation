using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UIKeyboardNavigator : MonoBehaviour
{
    [Tooltip("Which UI element should be selected first on Desktop?")]
    public GameObject firstSelected;

    void OnEnable()
    {
        if (IsDesktop() && EventSystem.current != null && firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void Update()
    {
        if (!IsDesktop() || EventSystem.current == null)
            return;

        var es = EventSystem.current;
        var current = es.currentSelectedGameObject ?? firstSelected;
        if (current == null) return;

        // TAB / SHIFT+TAB to move selection
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            bool backwards = Keyboard.current.shiftKey.isPressed;
            var sel = current.GetComponent<Selectable>();
            if (sel != null)
            {
                Selectable next = null;
                if (backwards)
                    next = sel.FindSelectableOnUp() ?? sel.FindSelectableOnLeft();
                else
                    next = sel.FindSelectableOnDown() ?? sel.FindSelectableOnRight();

                // wrap if needed
                if (next == null && firstSelected != null)
                    next = firstSelected.GetComponent<Selectable>();

                if (next != null)
                    es.SetSelectedGameObject(next.gameObject);
            }
        }

        // ENTER / SPACE to “click” the current button
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
            ExecuteEvents.Execute(current, new BaseEventData(es), ExecuteEvents.submitHandler);
    }

    static bool IsDesktop() =>
        GameSettings.Instance != null && GameSettings.Instance.CurrentMode == GameMode.Desktop;
}
