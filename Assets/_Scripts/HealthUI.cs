using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Image healthFill;
    private PlayerHealth playerHealth;

    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            playerHealth.OnPlayerDied += OnDie;
            UpdateHealthBar(playerHealth.currentHealth / playerHealth.maxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
            playerHealth.OnPlayerDied -= OnDie;
        }
    }

    private void UpdateHealthBar(float percentage)
    {
        if (healthFill == null) return;
        RectTransform rect = healthFill.rectTransform;
        rect.anchorMax = new Vector2(percentage, 1f);
        rect.offsetMax = new Vector2(-2, rect.offsetMax.y);
    }

    private void OnDie()
    {
        Debug.Log("El jugador ha muerto.");
    }
}
