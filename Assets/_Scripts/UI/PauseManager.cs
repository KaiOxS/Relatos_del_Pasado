using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [Header("Paneles de UI")]
    public GameObject pausePanel;

    private bool isPaused = false;

    private void Start()
    {
        // Asegurarse de que el panel de pausa esté desactivado al inicio
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Detectar la tecla de pausa (Escape) usando el nuevo Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f; // Congela el tiempo del juego
            isPaused = true;
        }
    }

    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f; // Reanuda el tiempo del juego
            isPaused = false;
        }
    }

    public void RestartCheckpoint()
    {
        // Es vital restaurar el tiempo antes de reintentar
        Time.timeScale = 1f;
        isPaused = false;
        
        if (pausePanel != null) pausePanel.SetActive(false);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartFromCheckpoint();
        }
    }

    public void QuitToMainMenu()
    {
        // Restaurar el tiempo antes de salir
        Time.timeScale = 1f; 
        isPaused = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneAsync("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
