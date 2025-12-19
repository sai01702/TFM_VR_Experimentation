using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Attach this directly to a Button to ensure it receives clicks.
/// Works around Input System issues.
/// </summary>
[RequireComponent(typeof(Button))]
public class SimpleButtonClick : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private bool isHovering = false;
    
    [SerializeField] bool enableDebug = true;

    private bool loggedMouseStatus = false;
    private int frameCount = 0;

    void Awake()
    {
        button = GetComponent<Button>();
        Debug.Log($"[SimpleButtonClick] Awake on {gameObject.name}");
    }

    void OnEnable()
    {
        Debug.Log($"[SimpleButtonClick] OnEnable on {gameObject.name}");
    }

    void Update()
    {
        frameCount++;
        
        // Log every 300 frames to show script is running
        if (frameCount % 300 == 0)
        {
            Debug.Log($"[SimpleButtonClick] Script is running on {gameObject.name}, frame {frameCount}");
        }

        var mouse = Mouse.current;
        
        // Log mouse status once
        if (!loggedMouseStatus)
        {
            loggedMouseStatus = true;
            if (mouse == null)
            {
                Debug.LogWarning("[SimpleButtonClick] Mouse.current is NULL! Are you in VR mode without mouse?");
            }
            else
            {
                Debug.Log("[SimpleButtonClick] Mouse.current is available");
            }
        }

        if (mouse == null) return;

        // Check EVERY frame if left button is pressed this frame
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            Debug.Log($"[SimpleButtonClick] LEFT CLICK DETECTED at {mousePos}");
            
            // Check if mouse is over this button using RectTransform
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                // Try with null camera (Screen Space Overlay)
                bool isOver = RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null);
                
                // Also try with main camera
                bool isOverWithCam = RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, Camera.main);
                
                Debug.Log($"[SimpleButtonClick] Over {gameObject.name}: {isOver} (with cam: {isOverWithCam})");
                Debug.Log($"[SimpleButtonClick] Button rect: {rect.rect}, position: {rect.position}");
                
                if (isOver || isOverWithCam)
                {
                    Debug.Log($"[SimpleButtonClick] INVOKING button click on {gameObject.name}!");
                    button?.onClick.Invoke();
                }
            }
        }
    }

    // Use OnGUI as a FALLBACK - this uses legacy input which might still work
    void OnGUI()
    {
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // GUI coordinates are flipped on Y axis
            Vector2 mousePos = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            Debug.Log($"[SimpleButtonClick] OnGUI CLICK at {mousePos}");
            
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                bool isOver = RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null);
                if (isOver)
                {
                    Debug.Log($"[SimpleButtonClick] OnGUI click over button - INVOKING!");
                    button?.onClick.Invoke();
                }
            }
        }
    }

    // IPointerClickHandler - called by EventSystem
    public void OnPointerClick(PointerEventData eventData)
    {
        if (enableDebug)
            Debug.Log($"[SimpleButtonClick] OnPointerClick on {gameObject.name}");
        // Button.onClick should already be invoked by Unity, but invoke manually as backup
        // button?.onClick.Invoke(); // Commented to avoid double-invoke
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (enableDebug)
            Debug.Log($"[SimpleButtonClick] Mouse entered {gameObject.name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (enableDebug)
            Debug.Log($"[SimpleButtonClick] Mouse exited {gameObject.name}");
    }
}

