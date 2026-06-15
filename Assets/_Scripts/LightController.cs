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

    private PlayerController player;

    private void Awake()
    {
        input = FindObjectOfType<InputManager>();
        player = GetComponentInParent<PlayerController>();
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

        UpdateOrientation();
    }

    private void UpdateOrientation()
    {
        if (flashlight == null || player == null) return;

        // Apuntar hacia la derecha (90 grados) o hacia la izquierda (-90 grados) en el eje Y
        float targetYRotation = player.facingRight ? 90f : -90f;
        flashlight.transform.parent.localRotation = Quaternion.Euler(0, targetYRotation, 0);
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
