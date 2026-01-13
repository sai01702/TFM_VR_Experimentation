
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Highlight Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.4f, 0.8f, 1f, 1f); // light blue
    public Color pressedColor = new Color(0.1f, 1f, 0.3f, 1f);   // green flash

    private Image image;
    private Coroutine flashRoutine;

    void Awake()
    {
        image = GetComponent<Image>();
        image.color = normalColor;
    }

    public void OnSelect(BaseEventData eventData)
    {
        image.color = highlightColor;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        image.color = normalColor;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashPress());
    }

    private System.Collections.IEnumerator FlashPress()
    {
        image.color = pressedColor;
        yield return new WaitForSeconds(0.15f);
        image.color = highlightColor;
    }
}
