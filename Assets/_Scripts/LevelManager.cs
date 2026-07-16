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

    public void RestartFromZero()
    {
        Time.timeScale = 1f;
        SaveSystem.ClearProgress(); // Limpiar todo progreso, inventario, vida, checkpoints
        
        // Volvemos a la cinemática inicial o menú principal para empezar de cero literalmente
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneAsync("Cinematica_Intro");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Cinematica_Intro");
        }
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

            // Apagar todos los demás Canvases de la escena para que nada se superponga
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            Canvas gameOverCanvas = gameOverPanel.GetComponentInParent<Canvas>();
            foreach (Canvas c in allCanvases)
            {
                // Ignorar el canvas del Game Over y evitar apagar el fondo negro de las transiciones
                if (c != gameOverCanvas && c.gameObject.name != "TransitionCanvas")
                {
                    c.enabled = false;
                }
            }

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
            RestartFromZero(); // Cambiado a RestartFromZero en vez de la misma escena
        }
    }

    public void RestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void ShowEndDemoScreen()
    {
        Time.timeScale = 0f; // Congelar el juego
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Crear Canvas desde cero en código para no tener que configurar nada en el Inspector
        GameObject canvasObj = new GameObject("EndDemoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Crear Fondo Negro translúcido
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image bgImg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0, 0, 0, 0.95f);
        RectTransform bgRt = bgImg.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Crear Texto de felicitación
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = "¡Fin de la demo, gracias por jugar!";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 50;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform txtRt = text.rectTransform;
        txtRt.anchorMin = new Vector2(0f, 0.4f);
        txtRt.anchorMax = new Vector2(1f, 0.7f);
        txtRt.sizeDelta = Vector2.zero;

        // Crear Botón de Salir
        GameObject btnObj = new GameObject("ExitButton");
        btnObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image btnImg = btnObj.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        UnityEngine.UI.Button btn = btnObj.AddComponent<UnityEngine.UI.Button>();
        
        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        UnityEngine.UI.Text btnText = btnTxtObj.AddComponent<UnityEngine.UI.Text>();
        btnText.text = "Volver al Menú Principal";
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 30;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        RectTransform btnTxtRt = btnText.rectTransform;
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.sizeDelta = Vector2.zero;

        RectTransform btnRt = btnImg.rectTransform;
        btnRt.anchorMin = new Vector2(0.3f, 0.2f);
        btnRt.anchorMax = new Vector2(0.7f, 0.35f);
        btnRt.sizeDelta = Vector2.zero;

        // Lógica del botón
        btn.onClick.AddListener(() => {
            Time.timeScale = 1f;
            SaveSystem.ClearProgress(); // Opcional: borrar guardado al terminar la demo
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });
    }
}
