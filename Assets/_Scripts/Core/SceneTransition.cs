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

    [Tooltip("(Opcional) Nombre del objeto vacío en la otra escena donde José debe aparecer. Si está vacío, aparecerá donde siempre.")]
    public string targetSpawnPointName;

    [Tooltip("(Opcional) Objeto que el jugador debe tener para desbloquear esta puerta (ej. Velo, Tripas). Dejar vacío si siempre está abierta.")]
    public string requiredItemToUnlock;

    [Tooltip("Mensaje que aparece en pantalla si intentas entrar sin el objeto necesario")]
    public string lockedMessage = "Necesitas recoger las tripas de Mariangula para avanzar";

    private bool isTransitioning = false;
    private bool showingMessage = false;
    private Image fadeImage;

    private void OnTriggerEnter(Collider other)
    {
        // Detectar si lo que entró a la cueva es el Jugador
        if (other.CompareTag("Player") && !isTransitioning)
        {
            // Protección contra bucles infinitos: si el jugador aparece exactamente encima de esta puerta,
            // no dejar que se active en los primeros 2 segundos de haber cargado el nivel.
            if (Time.timeSinceLevelLoad < 2f) return;
            if (!string.IsNullOrEmpty(requiredItemToUnlock))
            {
                if (InventorySystem.Instance == null) return;
                
                string needed = requiredItemToUnlock.Trim().ToLower();
                bool hasIt = false;
                foreach (var kvp in InventorySystem.Instance.items)
                {
                    if (kvp.Key.ToLower() == needed && kvp.Value > 0)
                    {
                        hasIt = true; 
                        break;
                    }
                }

                if (!hasIt)
                {
                    if (!showingMessage) StartCoroutine(ShowLockedMessage(lockedMessage));
                    return; // Bloquea el paso
                }
            }
            // Si especificaste un punto de destino, lo guardamos para que aparezca ahí. Si no, borramos el historial.
            if (!string.IsNullOrEmpty(targetSpawnPointName))
            {
                PlayerPrefs.SetString("LastCheckpoint", targetSpawnPointName);
                PlayerPrefs.Save();
            }
            else
            {
                PlayerPrefs.DeleteKey("LastCheckpoint");
            }
            
            StartCoroutine(TransitionRoutine());
        }
    }

    private IEnumerator ShowLockedMessage(string msg)
    {
        showingMessage = true;

        // Crear Canvas temporal
        GameObject canvasObj = new GameObject("LockedMessageCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        // Crear Texto
        GameObject textObj = new GameObject("LockedText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = msg;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // En versiones nuevas de Unity Arial.ttf a veces falla, LegacyRuntime.ttf es seguro o Arial.ttf
        if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        
        text.fontSize = 40;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        
        // Sombra/Borde para que se lea en fondos claros
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.1f, 0.4f);
        rt.anchorMax = new Vector2(0.9f, 0.6f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        yield return new WaitForSeconds(3f);
        
        if (canvasObj != null) Destroy(canvasObj);
        showingMessage = false;
    }

    private IEnumerator TransitionRoutine()
    {
        isTransitioning = true;

        // Congelar al jugador para que no siga cayendo o caminando mientras se oscurece
        CharacterController cc = FindObjectOfType<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Hacer al jugador invulnerable para que los enemigos no lo maten mientras carga
        PlayerHealth health = FindObjectOfType<PlayerHealth>();
        if (health != null) health.enabled = false; // Desactivar script de salud

        // Desactivar a la Dama Tapada o enemigos para evitar un InstaKill durante el fundido
        BaseEnemyAI[] enemies = FindObjectsOfType<BaseEnemyAI>();
        foreach (var enemy in enemies)
        {
            enemy.enabled = false;
        }

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
