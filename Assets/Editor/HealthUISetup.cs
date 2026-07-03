using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class HealthUISetup
{
    [MenuItem("Relatos/Autoconfigurar Barra de Salud")]
    public static void SetupHealthUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No se encontró un Canvas en la escena. Ejecuta primero el setup de UI de Estamina.");
            return;
        }

        // --- 1. Añadir PlayerHealth al jugador ---
        PlayerController player = Object.FindObjectOfType<PlayerController>();
        if (player != null)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph == null) ph = player.gameObject.AddComponent<PlayerHealth>();
            EditorUtility.SetDirty(player.gameObject);
        }

        // --- 2. Mover el inventario de piedras hacia abajo ---
        Transform stoneContainer = canvas.transform.Find("StoneUI_Container");
        if (stoneContainer != null)
        {
            RectTransform stoneRect = stoneContainer.GetComponent<RectTransform>();
            stoneRect.anchoredPosition = new Vector2(20, -90); // Bajar 70px más
            EditorUtility.SetDirty(stoneContainer.gameObject);
        }

        // --- 3. Limpiar barra de salud vieja si existe ---
        Transform oldHealth = canvas.transform.Find("HealthBar_BG");
        if (oldHealth != null) Object.DestroyImmediate(oldHealth.gameObject);

        // --- 4. Crear la barra de salud (fondo oscuro) ---
        GameObject healthBg = new GameObject("HealthBar_BG");
        healthBg.transform.SetParent(canvas.transform, false);
        Image bgImage = healthBg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.6f);

        RectTransform bgRect = healthBg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 1);
        bgRect.anchorMax = new Vector2(0, 1);
        bgRect.pivot = new Vector2(0, 1);
        bgRect.anchoredPosition = new Vector2(20, -20); // Top-left, 20px de margen
        bgRect.sizeDelta = new Vector2(220, 22);

        // --- 5. Crear el relleno rojo ---
        GameObject healthFill = new GameObject("HealthBar_Fill");
        healthFill.transform.SetParent(healthBg.transform, false);
        Image fillImage = healthFill.AddComponent<Image>();
        fillImage.color = new Color(0.85f, 0.1f, 0.1f, 1f); // Rojo

        RectTransform fillRect = healthFill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.offsetMin = new Vector2(2, 2);
        fillRect.offsetMax = new Vector2(-2, -2);

        // --- 6. Añadir HealthUI y conectar ---
        HealthUI healthUI = canvas.gameObject.GetComponent<HealthUI>();
        if (healthUI == null) healthUI = canvas.gameObject.AddComponent<HealthUI>();
        healthUI.healthFill = fillImage;

        EditorUtility.SetDirty(canvas.gameObject);
        Debug.Log("¡Barra de Salud configurada con éxito! El inventario de piedras se ha movido hacia abajo.");
    }
}
