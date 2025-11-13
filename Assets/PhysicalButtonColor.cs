using UnityEngine;

public class PhysicalButtonColor : MonoBehaviour
{
    [Header("Assign the MeshRenderer of the Press or Button part")]
    public MeshRenderer buttonRenderer;

    [Header("Button Colors")]
    public Color normalColor = Color.red;
    public Color pressedColor = Color.green;

    private Material buttonMaterial;

    private void Start()
    {
        if (buttonRenderer == null)
            buttonRenderer = GetComponent<MeshRenderer>();

        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            buttonMaterial.color = normalColor;
        }
    }

    // Call this when the button is pressed
    public void OnButtonPressed()
    {
        if (buttonMaterial != null)
            buttonMaterial.color = pressedColor;
    }

    // Call this when the button is released
    public void OnButtonReleased()
    {
        if (buttonMaterial != null)
            buttonMaterial.color = normalColor;
    }
}
