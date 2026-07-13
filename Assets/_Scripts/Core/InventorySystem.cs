using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance;

    public GameObject inventoryPanel;
    private Text inventoryText;

    // Diccionario para almacenar la cantidad de cada item
    public Dictionary<string, int> items = new Dictionary<string, int>();

    // Definición de ítems del GDD
    public enum ItemType 
    { 
        BotellaAlcanfor, Analgesicos, 
        BotellaVidrio, Piedra, PedazoVidrio, 
        Energizante, Esteroides,
        Tripas, Velo, Sombrero, PiedraFaltante, Pala
    }

    private bool inventoryOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        LoadInventory();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        inventoryPanel = null;
        inventoryOpen = false; // Por defecto cerrado al cambiar de nivel
        RefreshPanel();
    }

    private void Start()
    {
        RefreshPanel();
    }

    private void RefreshPanel()
    {
        if (inventoryPanel == null)
        {
            // Intentar encontrar el HUD_Canvas y el InventoryPanel existente
            GameObject hud = GameObject.Find("HUD_Canvas");
            if (hud != null)
            {
                Transform panelTransform = hud.transform.Find("InventoryPanel");
                if (panelTransform != null)
                {
                    inventoryPanel = panelTransform.gameObject;
                }
            }
            
            // Si el HUD_Canvas está desactivado o tiene otro nombre, búsqueda global
            if (inventoryPanel == null)
            {
                GameObject p = GameObject.Find("InventoryPanel");
                if (p != null) inventoryPanel = p;
            }
        }

        if (inventoryPanel != null)
        {
            UpdateInventoryUI();
            inventoryPanel.SetActive(inventoryOpen);
        }
        else
        {
            Debug.LogWarning("No se encontró el InventoryPanel en esta escena.");
        }
    }

    public void UpdateInventoryUI()
    {
        if (inventoryPanel == null) return;

        // Recopilar los slots (hijos del panel)
        List<Transform> slots = new List<Transform>();
        foreach (Transform child in inventoryPanel.transform)
        {
            slots.Add(child);
        }

        // Recopilamos los items que tenemos
        List<KeyValuePair<string, int>> validItems = new List<KeyValuePair<string, int>>();
        foreach (var kvp in items)
        {
            if (kvp.Value > 0)
            {
                validItems.Add(kvp);
            }
        }

        // Llenar los slots con los ítems (hasta 4 slots)
        for (int i = 0; i < slots.Count; i++)
        {
            Transform slot = slots[i];
            
            // Buscar el texto
            Text textComponent = slot.GetComponentInChildren<Text>();
            // Buscar la imagen del icono
            Transform iconTransform = slot.Find("ItemIcon");
            Image iconImg = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            if (textComponent == null)
            {
                GameObject txtObj = new GameObject("CantidadText");
                txtObj.transform.SetParent(slot, false);
                textComponent = txtObj.AddComponent<Text>();
                
                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (textComponent.font == null) textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                
                textComponent.fontSize = 20;
                textComponent.color = Color.white;
                textComponent.alignment = TextAnchor.LowerCenter;
                
                RectTransform rt = textComponent.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(1f, 0.3f);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;
            }

            if (iconImg == null)
            {
                GameObject imgObj = new GameObject("ItemIcon");
                imgObj.transform.SetParent(slot, false);
                iconImg = imgObj.AddComponent<Image>();
                iconImg.preserveAspect = true;
                
                RectTransform imgRt = iconImg.rectTransform;
                imgRt.anchorMin = new Vector2(0.1f, 0.3f);
                imgRt.anchorMax = new Vector2(0.9f, 0.9f);
                imgRt.sizeDelta = Vector2.zero;
                imgRt.anchoredPosition = Vector2.zero;
            }

            if (i < validItems.Count)
            {
                string itemName = validItems[i].Key;
                // Formatear el texto (Nombre y cantidad)
                textComponent.text = itemName + "\nx" + validItems[i].Value;
                
                // Asignar el sprite
                Sprite s = GetSpriteFor(itemName);
                if (s != null)
                {
                    iconImg.sprite = s;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = new Color(1,1,1,0); // Ocultar si no hay sprite
                }

                // Añadir lógica de click al slot
                Button btn = slot.GetComponent<Button>();
                if (btn == null) btn = slot.gameObject.AddComponent<Button>();
                
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnSlotClicked(itemName));
            }
            else
            {
                textComponent.text = ""; // Slot vacío
                iconImg.color = new Color(1,1,1,0); // Ocultar
                
                Button btn = slot.GetComponent<Button>();
                if (btn != null) btn.onClick.RemoveAllListeners();
            }
        }
    }

    private Sprite GetSpriteFor(string itemName)
    {
        string spriteName = itemName.ToLower();
        if (spriteName.Contains("botella") || spriteName.Contains("vidrio")) spriteName = "botella";
        
        Sprite s = Resources.Load<Sprite>("Sprites/Objects/" + spriteName);
        return s;
    }

    private void OnSlotClicked(string itemName)
    {
        PlayerHealth health = FindObjectOfType<PlayerHealth>();
        StaminaSystem stamina = FindObjectOfType<StaminaSystem>();

        switch (itemName)
        {
            case "Analgesicos":
            case "BotellaAlcanfor":
                if (health != null) health.Heal(health.maxHealth * 0.25f);
                UseItem(itemName);
                break;
            case "Energizante":
            case "Esteroides":
                if (stamina != null) stamina.SetUnlimitedStamina(1f);
                UseItem(itemName);
                break;
            case "BotellaVidrio":
            case "Piedra":
            case "PedazoVidrio":
                // No se consumen haciendo clic, se usan con el botón de lanzar.
                Debug.Log("Equipado/Listo para lanzar: " + itemName);
                break;
        }
    }

    public void AddItem(string itemName, int amount = 1)
    {
        if (items.ContainsKey(itemName))
        {
            items[itemName] += amount;
        }
        else
        {
            items[itemName] = amount;
        }
        SaveInventory();
        UpdateInventoryUI();
        Debug.Log("Objeto recogido: " + itemName + " (Total: " + items[itemName] + ")");
    }

    public bool HasItem(string itemName)
    {
        return items.ContainsKey(itemName) && items[itemName] > 0;
    }

    public void UseItem(string itemName)
    {
        if (HasItem(itemName))
        {
            items[itemName]--;
            SaveInventory();
            UpdateInventoryUI();
            Debug.Log("Objeto usado: " + itemName);
        }
    }

    private void SaveInventory()
    {
        // Guardado sencillo por ahora
        foreach(var kvp in items)
        {
            PlayerPrefs.SetInt("Inv_" + kvp.Key, kvp.Value);
        }
        PlayerPrefs.Save();
    }

    private void LoadInventory()
    {
        // Cargar ítems guardados si existen
        foreach (string name in System.Enum.GetNames(typeof(ItemType)))
        {
            if (PlayerPrefs.HasKey("Inv_" + name))
            {
                items[name] = PlayerPrefs.GetInt("Inv_" + name);
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        
        // Mantener TAB presionado para mostrar el inventario (se oculta al soltar)
        bool isTabPressed = Keyboard.current.tabKey.isPressed;
        if (isTabPressed != inventoryOpen)
        {
            inventoryOpen = isTabPressed;
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(inventoryOpen);
            }
        }

        // Teclas rápidas para consumir objetos
        if (Keyboard.current.digit1Key.wasPressedThisFrame) UseSlotByIndex(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) UseSlotByIndex(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) UseSlotByIndex(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) UseSlotByIndex(3);
    }

    private void UseSlotByIndex(int index)
    {
        List<KeyValuePair<string, int>> validItems = new List<KeyValuePair<string, int>>();
        foreach (var kvp in items)
        {
            if (kvp.Value > 0) validItems.Add(kvp);
        }
        
        if (index >= 0 && index < validItems.Count)
        {
            string itemName = validItems[index].Key;
            OnSlotClicked(itemName);
        }
    }
}