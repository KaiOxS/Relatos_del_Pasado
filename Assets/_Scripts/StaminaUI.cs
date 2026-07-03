using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public Image staminaFill;
    private StaminaSystem staminaSystem;

    private void Start()
    {
        // Buscar el sistema de estamina en la escena (normalmente en el jugador)
        staminaSystem = FindObjectOfType<StaminaSystem>();

        if (staminaSystem != null)
        {
            // Suscribirse al evento de cambio de estamina
            staminaSystem.OnStaminaChanged += UpdateStaminaBar;
            
            // Inicializar la barra visual con el valor actual al arrancar
            UpdateStaminaBar(staminaSystem.currentStamina / staminaSystem.maxStamina);
        }
        else
        {
            Debug.LogWarning("StaminaUI: No se encontró StaminaSystem en la escena.");
        }
    }

    private void OnDestroy()
    {
        if (staminaSystem != null)
        {
            staminaSystem.OnStaminaChanged -= UpdateStaminaBar;
        }
    }

    private void UpdateStaminaBar(float fillPercentage)
    {
        if (staminaFill != null)
        {
            // Usar fillAmount solo si hay un sprite asignado
            if (staminaFill.sprite != null && staminaFill.type == Image.Type.Filled)
            {
                staminaFill.fillAmount = fillPercentage;
            }
            else
            {
                // Fallback a prueba de fallos: escalar el RectTransform directamente
                RectTransform rect = staminaFill.rectTransform;
                rect.anchorMax = new Vector2(fillPercentage, 1);
                
                // Asegurar que los offsets se mantengan para no deformar el margen derecho
                rect.offsetMax = new Vector2(-2, rect.offsetMax.y);
            }
        }
    }
}
