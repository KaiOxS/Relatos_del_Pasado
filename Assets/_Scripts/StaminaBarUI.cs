using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conecta el StaminaSystem del jugador con la barra visual de UI.
/// Suscríbete al evento OnStaminaChanged para actualizar en tiempo real.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El componente Image que actúa como relleno de la barra (Image Type = Filled).")]
    public Image fillImage;

    [Tooltip("El CanvasGroup del panel completo para hacer fade.")]
    public CanvasGroup canvasGroup;

    [Header("Colores de la Barra")]
    public Color colorFull    = new Color(0.20f, 0.85f, 0.45f); // Verde esmeralda
    public Color colorMid     = new Color(0.95f, 0.65f, 0.10f); // Naranja
    public Color colorLow     = new Color(0.90f, 0.18f, 0.18f); // Rojo alerta

    [Header("Comportamiento Visual")]
    [Tooltip("Porcentaje bajo el cual la barra parpadea (0-1).")]
    public float blinkThreshold = 0.25f;
    public float blinkSpeed = 4f;

    [Tooltip("Segundos que la barra permanece visible antes de desvanecerse.")]
    public float visibleDuration = 3f;
    [Tooltip("Velocidad de desvanecimiento.")]
    public float fadeSpeed = 5f;
    [Tooltip("Alpha mínimo cuando está full y sin actividad.")]
    public float minAlpha = 0f;

    private StaminaSystem stamina;
    private float currentFill = 1f;
    private float displayTimer;
    private bool isExhaustedLastFrame;

    [System.Obsolete]
    private void Awake()
    {
        // Buscar el StaminaSystem en la escena
        stamina = FindObjectOfType<StaminaSystem>();
        if (stamina == null)
        {
            Debug.LogError("[StaminaBarUI] No se encontró StaminaSystem en la escena.");
            return;
        }

        // Suscribirse al evento
        stamina.OnStaminaChanged += OnStaminaChanged;
        stamina.OnStaminaDepleted += OnStaminaDepleted;
    }

    private void OnDestroy()
    {
        if (stamina == null) return;
        stamina.OnStaminaChanged -= OnStaminaChanged;
        stamina.OnStaminaDepleted -= OnStaminaDepleted;
    }

    private void Update()
    {
        if (fillImage == null) return;

        // Suavizar el relleno de la barra para que no sea brusco
        float smoothFill = Mathf.Lerp(fillImage.fillAmount, currentFill, Time.deltaTime * 8f);
        fillImage.fillAmount = smoothFill;

        // Color dinámico según nivel
        Color targetColor;
        if (currentFill > 0.5f)
            targetColor = Color.Lerp(colorMid, colorFull, (currentFill - 0.5f) * 2f);
        else
            targetColor = Color.Lerp(colorLow, colorMid, currentFill * 2f);

        fillImage.color = Color.Lerp(fillImage.color, targetColor, Time.deltaTime * 6f);

        // Parpadeo cuando está en zona de peligro
        if (currentFill <= blinkThreshold && canvasGroup != null)
        {
            float blink = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f; // 0 a 1
            canvasGroup.alpha = Mathf.Lerp(0.4f, 1f, blink);
            displayTimer = visibleDuration;
        }
        else if (canvasGroup != null)
        {
            // Solo es visible si no está al 100% (o si acaba de cambiar)
            if (currentFill < 0.999f || displayTimer > 0)
            {
                if (currentFill >= 0.999f && displayTimer > 0) displayTimer -= Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed);
            }
            else
            {
                // Se desvanece por completo cuando está llena
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, minAlpha, Time.deltaTime * fadeSpeed);
            }
        }
    }

    private void OnStaminaChanged(float normalizedValue)
    {
        currentFill = normalizedValue;
        displayTimer = visibleDuration; // Mostrar la barra al cambiar
    }

    private void OnStaminaDepleted()
    {
        currentFill = 0f;
        displayTimer = visibleDuration * 2f; // Mantener visible más tiempo al agotarse
    }
}
