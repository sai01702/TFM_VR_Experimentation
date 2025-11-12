using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHighlight : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [Header("Highlight Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.4f, 0.8f, 1f, 1f); // light blue border
    public Color pressedColor = new Color(0.1f, 1f, 0.3f, 1f);   // green flash

    [Header("Outline")]
    public Outline outline; // optional outline reference

    private Image image;
    private Coroutine flashRoutine;

    void Awake()
    {
        image = GetComponent<Image>();
        if (outline == null)
        {
            outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
        }

        outline.effectColor = normalColor;
        outline.effectDistance = new Vector2(4, 4);
        outline.enabled = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (outline != null)
        {
            outline.effectColor = highlightColor;
            outline.enabled = true;
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (outline != null)
            outline.enabled = false;
    }

    public void OnSubmit(BaseEventData eventData)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashPress());
    }

    private System.Collections.IEnumerator FlashPress()
    {
        if (outline != null)
            outline.effectColor = pressedColor;

        yield return new WaitForSeconds(0.15f);

        if (outline != null)
            outline.effectColor = highlightColor;
    }
}
