using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    public InventorySystem.ItemType itemType;
    public int amount = 1;
    public AudioClip pickupSound;
    public bool isArtifact = false;

    private bool playerInRange = false;
    private GameObject promptCanvas;
    private Transform mainCamTransform;

    private void Start()
    {
        CreatePrompt();
        if (Camera.main != null) mainCamTransform = Camera.main.transform;
    }

    private void CreatePrompt()
    {
        promptCanvas = new GameObject("PickupPrompt");
        promptCanvas.transform.SetParent(transform);
        promptCanvas.transform.localPosition = new Vector3(0, 1.2f, 0); // Un poco por encima del objeto
        
        Canvas canvas = promptCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Ajustar escala para que no se vea gigante en World Space
        RectTransform rt = promptCanvas.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(250, 50);
        promptCanvas.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
        
        UnityEngine.UI.Text text = promptCanvas.AddComponent<UnityEngine.UI.Text>();
        text.text = "R para recoger";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        text.fontSize = 25;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        
        UnityEngine.UI.Outline outline = promptCanvas.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
        
        promptCanvas.SetActive(false);
    }

    private void Update()
    {
        // Hacer que el texto siempre mire a la cámara
        if (promptCanvas != null && promptCanvas.activeSelf && mainCamTransform != null)
        {
            promptCanvas.transform.rotation = mainCamTransform.rotation;
        }

        // Si el jugador está cerca y presiona la letra R
        if (playerInRange && UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame)
            {
                Pickup();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (promptCanvas != null) promptCanvas.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (promptCanvas != null) promptCanvas.SetActive(false);
        }
    }

    private void Pickup()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(itemType.ToString(), amount);
            
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
            
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning("No hay InventorySystem en la escena para recoger el objeto.");
        }
    }
}
