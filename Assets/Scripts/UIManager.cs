using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class UIManager : MonoBehaviour
{
    // A reference to the UI Document we created in the scene.
    public UIDocument uiDocument;

    // A reference to the PlayerInventory script.
    public PlayerInventory playerInventory;

    // The UXML element we created in the UI Builder.
    private Label inventoryLabel;

    public PlacementUI placementUI;

    void Start()
    {
        if (uiDocument == null) return;
        placementUI.InitializeUI(uiDocument);
        // Get the root visual element from the UI Document.
        var root = uiDocument.rootVisualElement;

        // Find the label we named in the UI Builder.
        inventoryLabel = root.Q<Label>("inventoryLabel");

        if (inventoryLabel == null)
        {
            Debug.LogError("Could not find 'inventoryLabel' in the UI Document.");
            return;
        }

        // Update the UI at the start of the game.
        UpdateInventoryUI();
    }

    /// <summary>
    /// This function is called to update the UI with the latest inventory information.
    /// </summary>
    public void UpdateInventoryUI()
    {
        if (inventoryLabel == null || playerInventory == null) return;

        string inventoryText = "Inventory:\n";

        if (playerInventory.items.Count > 0)
        {
            foreach (var item in playerInventory.items)
            {
                inventoryText += $"{item.Key}: {item.Value}\n";
            }
        }
        else
        {
            inventoryText += "Empty";
        }

        inventoryLabel.text = inventoryText;
    }
}