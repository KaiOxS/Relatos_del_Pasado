using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PlayerUISetup
{
    [MenuItem("Relatos/Autoconfigurar Interfaz (Barra de Estamina)")]
    public static void SetupPlayerUI()
    {
        // 1. Buscar o crear EventSystem
        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // 2. Buscar o crear Canvas
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("PlayerCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // Si la barra ya existe, la eliminamos para recrearla limpia
        Transform oldBar = canvasObj.transform.Find("StaminaBar_BG");
        if (oldBar != null)
        {
            Object.DestroyImmediate(oldBar.gameObject);
        }

        // 3. Crear contenedor de la barra de estamina (Fondo Oscuro)
        GameObject staminaBarBg = new GameObject("StaminaBar_BG");
        staminaBarBg.transform.SetParent(canvasObj.transform, false);
        Image bgImage = staminaBarBg.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.6f); // Fondo semitransparente oscuro

        RectTransform bgRect = staminaBarBg.GetComponent<RectTransform>();
        // Posicionarlo en el Centro Superior (Top-Center)
        bgRect.anchorMin = new Vector2(0.5f, 1f);
        bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.anchoredPosition = new Vector2(0, -30f); // 30 pixels debajo del borde superior
        bgRect.sizeDelta = new Vector2(300f, 20f); // Ancho 300, Alto 20

        // 4. Crear barra de relleno (Celeste)
        GameObject staminaBarFill = new GameObject("StaminaBar_Fill");
        staminaBarFill.transform.SetParent(staminaBarBg.transform, false);
        Image fillImage = staminaBarFill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 1f, 1f); // Color Celeste
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;

        RectTransform fillRect = staminaBarFill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero; // Ocupa todo el tamaño de su padre (el BG)
        
        // Dejar un pequeño margen (borde) de 2 pixeles para que se vea un marco negro
        fillRect.offsetMin = new Vector2(2, 2); 
        fillRect.offsetMax = new Vector2(-2, -2);

        // 5. Añadir el script StaminaUI y conectar
        StaminaUI staminaUIScript = canvasObj.GetComponent<StaminaUI>();
        if (staminaUIScript == null)
        {
            staminaUIScript = canvasObj.AddComponent<StaminaUI>();
        }
        staminaUIScript.staminaFill = fillImage;

        EditorUtility.SetDirty(canvasObj);
        Debug.Log("¡Interfaz de Estamina configurada con éxito! La barra aparecerá arriba al centro.");
    }
}
