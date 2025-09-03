using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour
{
    [Header("UI Documents")]
    public UIDocument uiDocument;        // Inventory UI
    public UIDocument crosshairDocument; // Crosshair UI

    [Header("References")]
    public PlayerInventory playerInventory;
    public PlacementUI placementUI;

    private Label inventoryLabel;
    private Label crosshairLabel;

    void Start()
    {
        // --- Inventory UI ---
        if (uiDocument != null)
        {
            placementUI.InitializeUI(uiDocument);

            var root = uiDocument.rootVisualElement;
            inventoryLabel = root.Q<Label>("inventoryLabel");

            if (inventoryLabel == null)
                Debug.LogError("Could not find 'inventoryLabel' in the UI Document.");
            else
                UpdateInventoryUI();
        }

        // --- Crosshair UI ---
        if (crosshairDocument != null)
        {
            var crosshairRoot = crosshairDocument.rootVisualElement;
            crosshairLabel = crosshairRoot.Q<Label>("crosshairLabel");

            if (crosshairLabel == null)
                Debug.LogError("Could not find 'crosshairLabel' in Crosshair.uxml.");
        }
    }

    // Updates the inventory
    public void UpdateInventoryUI()
    {
        if (inventoryLabel == null || playerInventory == null) return;

        string inventoryText = "Inventory:\n";

        if (playerInventory.items.Count > 0)
        {
            foreach (var item in playerInventory.items)
                inventoryText += $"{item.Key}: {item.Value}\n";
        }
        else
        {
            inventoryText += "Empty";
        }

        inventoryLabel.text = inventoryText;
    }

    // --- Crosshair Controls ---
    public void ShowCrosshair(string text = "+")
    {
        if (crosshairLabel == null) return;
        crosshairLabel.text = text;
        crosshairLabel.style.display = DisplayStyle.Flex;
    }

    public void HideCrosshair()
    {
        if (crosshairLabel == null) return;
        crosshairLabel.style.display = DisplayStyle.None;
    }
}
