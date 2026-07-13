using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TumbaInteractable : MonoBehaviour
{
    [Header("Configuración de la Tumba")]
    [Tooltip("El texto de UI que muestra 'Presiona R para enterrar' (Opcional)")]
    public GameObject pressRText;
    
    [Tooltip("El modelo 3D de las tripas dentro de la tumba (Opcional). Ponlo en tu escena, acomódalo dentro de la tumba, y apágalo.")]
    public GameObject tripasVisual;

    private bool tripasDentro = false;
    private bool jugadorCerca = false;

    private void Start()
    {
        if (pressRText != null) pressRText.SetActive(false);
        if (tripasVisual != null) tripasVisual.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Detectar si el jugador se acerca a la tumba
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;
            CheckCondicionesYMostrarTexto();
        }

        // 2. Detectar si las tripas entran (caen/son lanzadas) físicamente
        CollectibleItem item = other.GetComponent<CollectibleItem>();
        if (item != null && item.itemType == InventorySystem.ItemType.Tripas)
        {
            tripasDentro = true;
            Debug.Log("Las tripas entraron a la tumba.");
            
            // Activamos el visual fijo de la tumba y destruimos el objeto que cayó para evitar bugs físicos
            if (tripasVisual != null) tripasVisual.SetActive(true);
            Destroy(other.gameObject);

            // Actualizar texto si el jugador ya estaba cerca
            CheckCondicionesYMostrarTexto();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            if (pressRText != null) pressRText.SetActive(false);
        }
    }

    private void CambiarTextoVisual(string nuevoTexto)
    {
        if (pressRText == null) return;
        
        // Intentar con Texto Legacy
        UnityEngine.UI.Text legacyText = pressRText.GetComponent<UnityEngine.UI.Text>();
        if (legacyText != null)
        {
            legacyText.text = nuevoTexto;
            return;
        }

        // Intentar con TextMeshPro
        TextMeshProUGUI tmproText = pressRText.GetComponent<TextMeshProUGUI>();
        if (tmproText != null)
        {
            tmproText.text = nuevoTexto;
        }
    }

    private void CheckCondicionesYMostrarTexto()
    {
        if (pressRText == null) return;

        if (jugadorCerca)
        {
            if (!tripasDentro && InventorySystem.Instance.HasItem("Tripas"))
            {
                pressRText.SetActive(true);
                CambiarTextoVisual("Presiona 'E' en la tumba para dejar las tripas.");
            }
            else if (tripasDentro && InventorySystem.Instance.HasItem("Pala"))
            {
                pressRText.SetActive(true);
                CambiarTextoVisual("Presiona 'R' para enterrar el pasado de Mariangula.");
            }
            else
            {
                pressRText.SetActive(false);
            }
        }
        else
        {
            pressRText.SetActive(false);
        }
    }

    private void Update()
    {
        if (jugadorCerca)
        {
            // Revisar constantemente por si recoge items mientras está cerca de la tumba
            CheckCondicionesYMostrarTexto();

            if (!tripasDentro)
            {
                // Si NO están las tripas, pero las tiene en el inventario
                if (InventorySystem.Instance != null && InventorySystem.Instance.HasItem("Tripas"))
                {
                    if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                    {
                        // Quitamos 1 de Tripas del inventario
                        InventorySystem.Instance.UseItem("Tripas");
                        tripasDentro = true;
                        
                        // Hacer que aparezcan físicamente en la tumba
                        if (tripasVisual != null) tripasVisual.SetActive(true);
                        
                        Debug.Log("Tripas colocadas en la tumba desde el inventario.");
                    }
                }
            }
            else
            {
                // Si ya están las tripas, y tiene la Pala
                if (InventorySystem.Instance != null && InventorySystem.Instance.HasItem("Pala"))
                {
                    if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                    {
                        EnterrarYTerminar();
                    }
                }
            }
        }
    }

    private void EnterrarYTerminar()
    {
        Debug.Log("¡Tripas enterradas! Fin de la demo.");
        
        if (pressRText != null) pressRText.SetActive(false);
        
        // Quitar la pala y las tripas lógicamente si queremos limpiar, pero el juego se acaba de todas formas
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ShowEndDemoScreen();
        }
        else
        {
            Debug.LogError("No se encontró un LevelManager en la escena para mostrar la pantalla final.");
        }
    }
}
