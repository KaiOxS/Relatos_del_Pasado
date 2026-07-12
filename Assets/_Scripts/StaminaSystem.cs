using UnityEngine;
using System;

public class StaminaSystem : MonoBehaviour
{
    [Header("Configuración Base")]
    public float maxStamina = 100f;
    public float currentStamina { get; private set; }
    
    [Header("Tasas de Consumo")]
    public float sprintDrainRate = 20f; // Estamina drenada por segundo en sprint
    public float wallJumpDrain = 15f;   // Coste fijo por salto de pared
    public float slideDrain = 10f;      // Coste fijo por deslizarse
    
    [Header("Recuperación")]
    public float regenRate = 15f;       // Recuperación por segundo en estado normal
    public float fatigueRegenRate = 5f; // Recuperación lenta si está en Fatiga Extrema
    public float regenDelay = 1.0f;     // Tiempo de espera sin consumir antes de regenerar
    
    private float regenTimer = 0f;
    private float unlimitedStaminaTimer = 0f;
    public bool IsExhausted { get; private set; } // Estado de "Fatiga Extrema" del GDD

    // Eventos para UI y Audio
    public Action OnStaminaDepleted;
    public Action<float> OnStaminaChanged; // Pasa el porcentaje de 0 a 1

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (unlimitedStaminaTimer > 0)
        {
            unlimitedStaminaTimer -= Time.deltaTime;
            currentStamina = maxStamina;
            IsExhausted = false;
            OnStaminaChanged?.Invoke(1f);
        }
        else
        {
            HandleRegeneration();
        }
    }

    private void HandleRegeneration()
    {
        if (regenTimer > 0)
        {
            regenTimer -= Time.deltaTime;
        }
        else if (currentStamina < maxStamina)
        {
            float rate = IsExhausted ? fatigueRegenRate : regenRate;
            currentStamina += rate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            
            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
            
            // Según GDD, sale de la fatiga al recuperarse (aquí configurado al 50%)
            if (IsExhausted && currentStamina >= maxStamina * 0.5f) 
            {
                IsExhausted = false;
            }
        }
    }

    public void SetUnlimitedStamina(float duration)
    {
        unlimitedStaminaTimer = duration;
        currentStamina = maxStamina;
        IsExhausted = false;
        OnStaminaChanged?.Invoke(1f);
    }

    public void ConsumeStamina(float amount)
    {
        if (unlimitedStaminaTimer > 0) return;
        if (IsExhausted) return;

        currentStamina -= amount;
        regenTimer = regenDelay; // Reinicia el temporizador de recuperación

        if (currentStamina <= 0)
        {
            currentStamina = 0;
            IsExhausted = true;
            OnStaminaDepleted?.Invoke(); // Dispara evento de fatiga (útil para audio)
        }
        
        OnStaminaChanged?.Invoke(currentStamina / maxStamina);
    }
    
    public void ConsumeStaminaOverTime(float rate)
    {
         ConsumeStamina(rate * Time.deltaTime);
    }
    
    public bool HasEnoughStamina(float amount)
    {
        return !IsExhausted && currentStamina >= amount;
    }
}
