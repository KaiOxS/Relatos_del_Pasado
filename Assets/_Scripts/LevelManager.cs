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
        string checkpointName = PlayerPrefs.GetString("LastCheckpoint", "").Trim();
        if (!string.IsNullOrEmpty(checkpointName))
        {
            GameObject checkpointObj = GameObject.Find(checkpointName);
            if (checkpointObj == null)
            {
                // Intentar buscar checkpoints inactivos
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (Transform t in allTransforms)
                {
                    if (t.name.Trim() == checkpointName && t.gameObject.scene.isLoaded)
                    {
                        checkpointObj = t.gameObject;
                        break;
                    }
                }
            }

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
        if (gameOverPanel == null)
        {
            // Método más seguro: buscar dentro del HUD_Canvas que sabemos que está en la escena
            GameObject hud = GameObject.Find("HUD_Canvas");
            if (hud != null)
            {
                Transform t = hud.transform.Find("GameOverPanel");
                if (t == null) t = hud.transform.Find("Panel_GameOver"); // Nombres comunes
                if (t == null) t = hud.transform.Find("Game_Over");
                
                if (t != null)
                {
                    gameOverPanel = t.gameObject;
                }
            }
        }

        if (gameOverPanel != null)
        {
            // Borrar todo al morir según petición del usuario
            SaveSystem.ClearProgress();

            gameOverPanel.SetActive(true);
            Time.timeScale = 0f; // Congelar el juego
            Debug.Log("Game Over Panel Activado.");
            
            // Forzar que el cursor sea visible por si necesitan dar clic en un botón de reintentar
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("No hay un panel de Game Over en la escena. Recargando de inmediato...");
            SaveSystem.ClearProgress();
            RestartScene();
        }
    }

    public void RestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
