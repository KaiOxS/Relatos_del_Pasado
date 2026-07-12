using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuración")]
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    public Action<float> OnHealthChanged; // Pasa porcentaje de 0 a 1
    public Action OnPlayerDied;

    private bool isDead = false;

    public AudioSource audioSource;

    public AudioClip hurtClip;

    private void Awake()
    {
        if (PlayerPrefs.HasKey("PlayerHealth"))
        {
            currentHealth = PlayerPrefs.GetFloat("PlayerHealth");
            if (currentHealth <= 0f)
            {
                currentHealth = maxHealth;
                PlayerPrefs.SetFloat("PlayerHealth", currentHealth);
                PlayerPrefs.Save();
            }
        }
        else
        {
            currentHealth = maxHealth;
            PlayerPrefs.SetFloat("PlayerHealth", currentHealth);
            PlayerPrefs.Save();
        }
    }

    private void Start()
    {
        // Conectar la muerte del jugador con el LevelManager
        OnPlayerDied += HandleDeath;
    }

    private void OnDestroy()
    {
        OnPlayerDied -= HandleDeath;
    }

    private void HandleDeath()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ShowGameOver();
        }
        else
        {
            // Fallback si no hay LevelManager
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    public void TakeDamage(float amount)
    {
        audioSource.PlayOneShot(hurtClip);
        if (isDead) return;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.TriggerDamageAnimation();

        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        PlayerPrefs.SetFloat("PlayerHealth", currentHealth);
        PlayerPrefs.Save();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        if (currentHealth <= 0)
        {
            isDead = true;
            OnPlayerDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        PlayerPrefs.SetFloat("PlayerHealth", currentHealth);
        PlayerPrefs.Save();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        PlayerPrefs.SetFloat("PlayerHealth", currentHealth);
        PlayerPrefs.Save();
        OnHealthChanged?.Invoke(1f);
    }
}
