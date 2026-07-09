using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public Transform currentCheckpoint;
    private PlayerController player;

    [Header("UI Panels")]
    public GameObject gameOverPanel;

    [Header("Debug / Pruebas")]
    [Tooltip("Activa esto si quieres que el juego olvide el punto de control guardado y empiece desde el inicio siempre.")]
    public bool resetSaveOnStart = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();

        // Borrar guardado si estamos haciendo pruebas y queremos empezar desde 0
        if (resetSaveOnStart)
        {
            PlayerPrefs.DeleteKey("LastCheckpoint");
            PlayerPrefs.Save();
        }

        // Si hay un checkpoint guardado, movemos al jugador ahí al iniciar la escena
        string checkpointName = PlayerPrefs.GetString("LastCheckpoint", "");
        if (!string.IsNullOrEmpty(checkpointName))
        {
            GameObject checkpointObj = GameObject.Find(checkpointName);
            if (checkpointObj != null)
            {
                currentCheckpoint = checkpointObj.transform;
                if (player != null)
                {
                    // Desactivar temporalmente el controller para forzar el teletransporte
                    CharacterController cc = player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    
                    player.transform.position = currentCheckpoint.position;
                    
                    if (cc != null) cc.enabled = true;
                    
                    // Asegurar que la cámara también haga teletransporte de inmediato
                    CameraController cam = FindAnyObjectByType<CameraController>();
                    if (cam != null) cam.SnapToTarget();
                }
            }
        }
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
        PlayerPrefs.SetString("LastCheckpoint", newCheckpoint.name);
        PlayerPrefs.Save();
        Debug.Log("Checkpoint Guardado: " + newCheckpoint.name);
    }

    public void RestartFromCheckpoint()
    {
        Time.timeScale = 1f; // Restaurar el tiempo antes de recargar
        RestartScene(); // Recargamos la escena para reiniciar a los enemigos y objetos
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; // Congelar el juego
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
