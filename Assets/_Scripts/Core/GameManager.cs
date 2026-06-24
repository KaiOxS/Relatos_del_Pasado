using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingScreenPrefab;
    private GameObject currentLoadingScreen;
    private Slider loadingSlider;

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

    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // Instanciar la pantalla de carga si existe un prefab
        if (loadingScreenPrefab != null)
        {
            currentLoadingScreen = Instantiate(loadingScreenPrefab);
            DontDestroyOnLoad(currentLoadingScreen);
            
            // Buscar el Slider en el prefab de carga
            loadingSlider = currentLoadingScreen.GetComponentInChildren<Slider>();
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            yield return null;
        }

        // Destruir la pantalla de carga cuando termine
        if (currentLoadingScreen != null)
        {
            Destroy(currentLoadingScreen);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
