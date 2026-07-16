using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Title Setup")]
    [Tooltip("El sprite 'Titulo' de Assets/Sprites")]
    [SerializeField] private Sprite tituloSprite;
    [Tooltip("Tamaño en pixeles del título en la pantalla")]
    [SerializeField] private Vector2 tituloSize = new Vector2(500f, 180f);
    [Tooltip("Posición Y del título respecto al centro")]
    [SerializeField] private float tituloPosY = 200f;
    [Tooltip("Cuánto desplazar el menú hacia abajo si no se usa LayoutGroup")]
    [SerializeField] private float menuShiftY = -120f;

    [Header("Level Loading")]
    [SerializeField] private string newGameSceneName = "Cinematica_Intro";

    private void Start()
    {
        // Asegurarse de que el panel principal esté activo y los demás ocultos
        ShowMainMenu();
        SetupTitle();
    }

    private void SetupTitle()
    {
        if (tituloSprite == null || mainMenuPanel == null) return;

        // Evitar duplicados
        if (mainMenuPanel.transform.Find("DynamicTitleImage") != null) return;

        GameObject titleObj = new GameObject("DynamicTitleImage");
        titleObj.transform.SetParent(mainMenuPanel.transform, false);

        Image img = titleObj.AddComponent<Image>();
        img.sprite = tituloSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        RectTransform titleRt = img.rectTransform;
        
        // Centrado en el panel
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = tituloSize;
        titleRt.anchoredPosition = new Vector2(0f, tituloPosY);

        // Si el panel de menú tiene un componente de Layout, dejamos que se auto-organice
        LayoutGroup layout = mainMenuPanel.GetComponent<LayoutGroup>();
        if (layout != null)
        {
            titleObj.transform.SetAsFirstSibling();
        }
        else
        {
            // Si no hay layout group, movemos los otros botones/elementos hacia abajo
            foreach (Transform child in mainMenuPanel.transform)
            {
                if (child.gameObject == titleObj) continue;

                RectTransform childRt = child.GetComponent<RectTransform>();
                if (childRt != null)
                {
                    childRt.anchoredPosition += new Vector2(0f, menuShiftY);
                }
            }
        }
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
