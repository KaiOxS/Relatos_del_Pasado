using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Level Loading")]
    [SerializeField] private string newGameSceneName = "Level1";

    private void Start()
    {
        // Asegurarse de que el panel principal esté activo y los demás ocultos
        ShowMainMenu();
    }

    public void OnNewGameClicked()
    {
        if (GameManager.Instance != null)
        {
            // Resetear cualquier progreso guardado si es necesario
            SaveSystem.ClearProgress();
            GameManager.Instance.LoadSceneAsync(newGameSceneName);
        }
        else
        {
            Debug.LogError("GameManager no encontrado. Asegúrate de tener un GameManager en la escena.");
        }
    }

    public void OnContinueClicked()
    {
        string lastSavedScene = SaveSystem.GetLastSavedScene();
        if (!string.IsNullOrEmpty(lastSavedScene))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSceneAsync(lastSavedScene);
            }
        }
        else
        {
            Debug.Log("No hay partida guardada. Iniciando nueva partida.");
            OnNewGameClicked();
        }
    }

    public void OnSettingsClicked()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnExitClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            Application.Quit();
            Debug.Log("Juego cerrado (solo funciona en build).");
        }
    }
}
