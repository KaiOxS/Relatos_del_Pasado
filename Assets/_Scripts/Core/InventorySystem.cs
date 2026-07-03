using UnityEngine;
using UnityEngine.InputSystem;

public class InventorySystem : MonoBehaviour
{
    public GameObject inventoryPanel;

    private bool inventoryOpen = false;

    private void Start()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    private void ToggleInventory()
    {
        inventoryOpen = !inventoryOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(inventoryOpen);
        }
    }
}