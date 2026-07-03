using UnityEngine;
using UnityEngine.UI;

public class StoneUI : MonoBehaviour
{
    public Text stoneText;
    private ThrowSystem throwSystem;

    private void Start()
    {
        throwSystem = FindObjectOfType<ThrowSystem>();
        if (throwSystem != null)
        {
            throwSystem.OnStonesChanged += UpdateText;
            UpdateText(throwSystem.stoneCount);
        }
        else
        {
            Debug.LogWarning("StoneUI: No se encontró ThrowSystem en la escena.");
        }
    }

    private void OnDestroy()
    {
        if (throwSystem != null)
        {
            throwSystem.OnStonesChanged -= UpdateText;
        }
    }

    private void UpdateText(int count)
    {
        if (stoneText != null)
        {
            stoneText.text = count.ToString();
        }
    }
}
