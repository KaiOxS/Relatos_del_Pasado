using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [System.Serializable]
    public struct TutorialStep
    {
        public string message;
        public Sprite icon;
        public float duration;
    }

    [Header("UI References")]
    public Text tutorialText;
    public Image tutorialImage;
    public GameObject tutorialPanel;

    [Header("Tutorial Sequence")]
    public TutorialStep[] steps;
    public float fadeDuration = 0.5f; // Tiempo que dura el desvanecimiento

    private void Start()
    {
        // Revisar si ya se mostró el tutorial en esta partida
        if (PlayerPrefs.GetInt("TutorialShown", 0) == 1)
        {
            if (tutorialPanel != null) tutorialPanel.SetActive(false);
            return;
        }

        if (steps != null && steps.Length > 0)
        {
            PlayerPrefs.SetInt("TutorialShown", 1);
            PlayerPrefs.Save();
            StartCoroutine(ShowTutorialRoutine());
        }
        else
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }
    }

    private IEnumerator ShowTutorialRoutine()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        // Asegurarnos de que el alpha empiece en 0 si queremos que el primero también aparezca con fade
        SetAlpha(0f);

        foreach (var step in steps)
        {
            // 1. Configurar el texto e imagen del nuevo paso
            if (tutorialText != null) tutorialText.text = step.message;
            
            if (tutorialImage != null)
            {
                if (step.icon != null)
                {
                    tutorialImage.sprite = step.icon;
                    tutorialImage.gameObject.SetActive(true);
                }
                else
                {
                    tutorialImage.gameObject.SetActive(false);
                }
            }

            // 2. Desvanecimiento de entrada (Fade In)
            yield return StartCoroutine(FadeRoutine(1f));

            // 3. Esperar el tiempo de lectura
            yield return new WaitForSeconds(step.duration);

            // 4. Desvanecimiento de salida (Fade Out) antes de pasar al siguiente
            yield return StartCoroutine(FadeRoutine(0f));
        }

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float elapsedTime = 0f;
        
        Color textStartColor = tutorialText != null ? tutorialText.color : Color.white;
        Color imageStartColor = tutorialImage != null ? tutorialImage.color : Color.white;
        
        Color textTargetColor = new Color(textStartColor.r, textStartColor.g, textStartColor.b, targetAlpha);
        Color imageTargetColor = new Color(imageStartColor.r, imageStartColor.g, imageStartColor.b, targetAlpha);

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;

            if (tutorialText != null) tutorialText.color = Color.Lerp(textStartColor, textTargetColor, t);
            if (tutorialImage != null && tutorialImage.gameObject.activeSelf) tutorialImage.color = Color.Lerp(imageStartColor, imageTargetColor, t);

            yield return null;
        }

        // Asegurar el valor final
        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (tutorialText != null)
        {
            Color c = tutorialText.color;
            c.a = alpha;
            tutorialText.color = c;
        }
        if (tutorialImage != null)
        {
            Color c = tutorialImage.color;
            c.a = alpha;
            tutorialImage.color = c;
        }
    }
}
