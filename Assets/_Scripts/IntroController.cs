using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Coloca este script en un GameObject vacío de la escena "Cinematica_Intro".
/// Arrastra el VideoPlayer al campo videoPlayer y asigna el VideoClip directamente en el VideoPlayer.
/// El script crea todo lo visual automáticamente (Canvas, RawImage, RenderTexture).
/// IMPORTANTE: En el componente VideoPlayer del Inspector:
///   - "Play On Awake" -> DESACTIVADO
///   - "Wait For First Frame" -> ACTIVADO
///   - "Render Mode" -> Render Texture (el script lo sobreescribe de todas formas)
/// </summary>
public class IntroController : MonoBehaviour
{
    [Header("Video")]
    [Tooltip("Arrastra aquí el componente VideoPlayer del mismo objeto o de la escena.")]
    public VideoPlayer videoPlayer;

    [Header("Escena destino")]
    public string gameSceneName = "Greybox_Prototipo";

    [Header("Skip")]
    public bool allowSkip = true;

    private RenderTexture renderTexture;
    private bool isLoading = false;
    private RawImage videoDisplay;

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[IntroController] Falta asignar el VideoPlayer.");
            LoadGame();
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Crear RenderTexture del tamaño de la pantalla
        renderTexture = new RenderTexture(Screen.width, Screen.height, 0);

        // Configurar VideoPlayer para usar la RenderTexture
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = false;
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        // Crear UI que muestra la RenderTexture a pantalla completa
        CreateVideoCanvas();

        // Suscribirse y preparar
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Prepare();
    }

    private void CreateVideoCanvas()
    {
        // Canvas
        GameObject canvasObj = new GameObject("VideoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // RawImage que cubre toda la pantalla
        GameObject rawObj = new GameObject("VideoDisplay");
        rawObj.transform.SetParent(canvasObj.transform, false);
        videoDisplay = rawObj.AddComponent<RawImage>();
        videoDisplay.texture = renderTexture;
        videoDisplay.color = Color.black; // Negro hasta que empiece el video

        RectTransform rt = videoDisplay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Texto de Skip (opcional)
        if (allowSkip)
        {
            GameObject skipObj = new GameObject("SkipText");
            skipObj.transform.SetParent(canvasObj.transform, false);
            Text skipLabel = skipObj.AddComponent<Text>();
            skipLabel.text = "Pulsa [Esc] o [Espacio] para saltar";
            skipLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipLabel.fontSize = 18;
            skipLabel.color = new Color(1, 1, 1, 0.6f);
            skipLabel.alignment = TextAnchor.LowerRight;

            RectTransform skipRt = skipLabel.rectTransform;
            skipRt.anchorMin = new Vector2(0, 0);
            skipRt.anchorMax = new Vector2(1, 0);
            skipRt.offsetMin = new Vector2(0, 20);
            skipRt.offsetMax = new Vector2(-20, 50);
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnVideoPrepared;

        // Ahora que está listo, poner el display en blanco para que se vea
        if (videoDisplay != null)
            videoDisplay.color = Color.white;

        vp.Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        LoadGame();
    }

    private void Update()
    {
        if (!allowSkip || isLoading) return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            LoadGame();
    }

    private void LoadGame()
    {
        if (isLoading) return;
        isLoading = true;

        if (videoPlayer != null) videoPlayer.Stop();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        // Fundido a negro antes de cargar
        GameObject fadeObj = new GameObject("FadeCanvas");
        Canvas fadeCanvas = fadeObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;

        GameObject imgObj = new GameObject("Fade");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);

        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        float elapsed = 0f;
        float duration = 1f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        if (renderTexture != null) renderTexture.Release();
        SceneManager.LoadScene(gameSceneName);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
        if (renderTexture != null) renderTexture.Release();
    }
}
