using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public Transform currentCheckpoint;
    private PlayerController player;

    [Header("UI Panels")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        player = FindObjectOfType<PlayerController>();
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
        Debug.Log("Checkpoint Guardado: " + newCheckpoint.name);
    }

    public void RestartFromCheckpoint()
    {
        if (player != null && currentCheckpoint != null)
        {
            // Ocultar Game Over
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Resetear posición y estados
            player.transform.position = currentCheckpoint.position;
            // TODO: Restablecer estamina y curar estados
            Debug.Log("Jugador reiniciado desde el Checkpoint.");
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("Game Over Panel Activado.");
        }
        else
        {
            Debug.LogWarning("No hay un panel de Game Over asignado en el LevelManager.");
            // Si no hay panel, recargamos la escena directamente por precaución
            RestartScene();
        }
    }

    public void RestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
