using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlacementUI : MonoBehaviour
{
    public VisualElement placementPanel;
    private VisualElement itemsContainer;

    // Public reference to the LevitateSpell script
    public LevitateSpell levitateSpell;

    // Public reference to the player's inventory
    public PlayerInventory playerInventory;

    public void InitializeUI(UIDocument uiDocument)
    {
        var root = uiDocument.rootVisualElement;
        placementPanel = root.Q<VisualElement>("PlacementPanel");
        itemsContainer = placementPanel.Q<VisualElement>("itemsContainer");

        placementPanel.style.display = DisplayStyle.None;
    }

    public void TogglePlacementPanel()
    {
        if (placementPanel.style.display == DisplayStyle.None)
        {
            PopulatePlacementButtons();
            placementPanel.style.display = DisplayStyle.Flex;
            levitateSpell.ToggleCursor(false); // Unlock cursor when UI is open
        }
        else
        {
            placementPanel.style.display = DisplayStyle.None;
            levitateSpell.ExitPlacementMode(); // Exit placement and lock cursor
        }
    }

    private void PopulatePlacementButtons()
    {
        itemsContainer.Clear();

        foreach (var item in playerInventory.items)
        {
            if (item.Value > 0)
            {
                Button newButton = new Button();
                newButton.text = item.Key;
                newButton.name = $"{item.Key}Button";

                newButton.clicked += () => OnItemClick(item.Key);

                itemsContainer.Add(newButton);
            }
        }
    }

    private void OnItemClick(string itemName)
    {
        // This is where the item is selected.
        levitateSpell.EnterPlacementMode(itemName);

        // Hide the UI panel
        placementPanel.style.display = DisplayStyle.None;
    }
}
