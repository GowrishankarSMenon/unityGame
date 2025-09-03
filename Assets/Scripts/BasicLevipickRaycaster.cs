using UnityEngine;
using UnityEngine.UIElements;

public class BasicLevipickRaycaster : MonoBehaviour
{
    public Camera playerCamera;
    public UIDocument uiDocument;   // Assign in Inspector
    private VisualElement crosshairUI;

    public float maxDistance = 10f;
    public bool debugMode = true;

    void Start()
    {
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            crosshairUI = root.Q<Label>("crosshairLabel"); // matches your UI Builder
        }
    }

    void Update()
    {
        if (playerCamera == null || crosshairUI == null) return;

        if (crosshairUI.worldBound.width <= 0 || crosshairUI.worldBound.height <= 0)
        {
            // Layout not ready yet, skip this frame
            return;
        }

        Vector2 panelPos = crosshairUI.worldBound.center;
        Vector2 screenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Debug.Log($"PanelPos: {panelPos}, ScreenPos: {screenPos}");


        // Step 3: Cast ray
        Ray ray = playerCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            if (hit.collider.CompareTag("Levipick"))
            {
                if (debugMode)
                {
                    Debug.Log("Hit Levipick object: " + hit.collider.name);
                    crosshairUI.style.color = Color.red; // highlight crosshair
                }
            }
            else
            {
                crosshairUI.style.color = Color.black;
            }
        }
        else
        {
            crosshairUI.style.color = Color.black;
        }
    }
}
