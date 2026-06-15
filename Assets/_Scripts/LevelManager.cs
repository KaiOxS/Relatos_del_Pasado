using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    public Transform currentCheckpoint;
    private PlayerController player;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
            // Resetear posición y estados
            player.transform.position = currentCheckpoint.position;
            // TODO: Restablecer estamina y curar estados
            Debug.Log("Jugador reiniciado desde el Checkpoint.");
        }
    }

    public void RestartScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
