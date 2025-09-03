using UnityEngine;
using UnityEngine.UIElements;

public class CrosshairUI : MonoBehaviour
{
    public UIDocument uiDocument;

    private VisualElement crosshair;
    private Label interactPrompt;

    void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("CrosshairUI: No UIDocument assigned!");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // Find UI elements by the names you set in UI Builder
        crosshair = root.Q<VisualElement>("Crosshair");
        interactPrompt = root.Q<Label>("InteractPrompt");

        if (crosshair == null || interactPrompt == null)
        {
            Debug.LogError("CrosshairUI: Could not find Crosshair or InteractPrompt in UXML!");
        }

        HidePrompt(); // make sure it's hidden at start
    }

    public void ShowPrompt(string text)
    {
        if (interactPrompt != null)
        {
            interactPrompt.text = text;
            interactPrompt.style.display = DisplayStyle.Flex;
        }
    }

    public void HidePrompt()
    {
        if (interactPrompt != null)
        {
            interactPrompt.style.display = DisplayStyle.None;
        }
    }
}
