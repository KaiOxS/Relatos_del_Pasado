using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Escribe el nombre EXACTO de la siguiente escena (ej. Level2_Subterraneo)")]
    public string sceneToLoad;
    
    [Tooltip("Tiempo en segundos que tarda la pantalla en ponerse negra")]
    public float fadeDuration = 1.5f;

    private bool isTransitioning = false;
    private Image fadeImage;

    private void OnTriggerEnter(Collider other)
    {
        // Detectar si lo que entró a la cueva es el Jugador
        if (other.CompareTag("Player") && !isTransitioning)
        {
            // Borramos el último punto de control para que no aparezca en el mapa viejo si muere
            PlayerPrefs.DeleteKey("LastCheckpoint");
            
            StartCoroutine(TransitionRoutine());
        }
    }

    private IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        // Congelar al jugador para que no siga cayendo o caminando mientras se oscurece
        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Crear una pantalla negra automáticamente (sin configurar UI a mano)
        CreateFadeCanvas();

        // Animar el fundido a negro (Fade Out)
        float elapsed = 0f;
        Color colorNegro = fadeImage.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            colorNegro.a = Mathf.Clamp01(elapsed / fadeDuration); // Va de 0 a 1
            fadeImage.color = colorNegro;
            yield return null;
        }

        // Cargar el nuevo nivel
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("¡ERROR! No pusiste el nombre de la siguiente escena en el Inspector.");
        }
    }

    private void CreateFadeCanvas()
    {
        // Crear Canvas
        GameObject canvasObj = new GameObject("TransitionCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Asegura que esté por encima de todos los menús

        // Crear Imagen Negra
        GameObject imageObj = new GameObject("BlackScreen");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Empieza 100% transparente
        
        // Estirar la imagen para que cubra toda la pantalla
        RectTransform rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }
}
