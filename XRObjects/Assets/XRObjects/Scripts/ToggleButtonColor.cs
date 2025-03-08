using UnityEngine;
using UnityEngine.UI;

public class ToggleButtonColor : MonoBehaviour
{
    // Colors to toggle between.
    public Color colorA = Color.white;
    public Color colorB = Color.red;

    // Tracks the current color state.
    private bool isColorA = true;

    // Cached reference to the Image component.
    private Image buttonImage;

    void Start()
    {
        // Attempt to get the Image component on the same GameObject.
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogWarning("ToggleButtonColor: No Image component found on this GameObject.");
        }
        else
        {
            // Initialize the button with colorA.
            buttonImage.color = colorA;
        }
    }

    // This method should be called from the Button's OnClick event.
    public void ToggleColor()
    {
        if (buttonImage != null)
        {
            // Toggle the color.
            buttonImage.color = isColorA ? colorB : colorA;
            isColorA = !isColorA;
            Debug.Log("Button toggled to " + buttonImage.color);
        }
    }
}