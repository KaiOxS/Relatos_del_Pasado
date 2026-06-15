using UnityEngine;
using System;

public class LightController : MonoBehaviour
{
    [Header("Configuración Linterna")]
    public Light flashlight;
    public bool isFlashlightOn { get; private set; } = true;
    
    private InputManager input;

    // Evento para que Mariangula (IA) escuche si la luz le está apuntando
    public Action<bool> OnFlashlightToggled;

    private void Awake()
    {
        input = FindObjectOfType<InputManager>();
        if (flashlight != null)
        {
            isFlashlightOn = flashlight.enabled;
        }
    }

    private void Update()
    {
        if (input.FlashlightTriggered)
        {
            ToggleFlashlight();
            input.ConsumeFlashlight();
        }
    }

    private void ToggleFlashlight()
    {
        if (flashlight != null)
        {
            isFlashlightOn = !isFlashlightOn;
            flashlight.enabled = isFlashlightOn;
            OnFlashlightToggled?.Invoke(isFlashlightOn);
        }
    }
}
