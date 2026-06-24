using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider volumeSlider;
    // Puedes agregar más configuraciones aquí, como Dropdowns de resolución, Toggles de pantalla completa, etc.

    [Header("References")]
    [SerializeField] private MainMenu mainMenuReference;

    private const string VOLUME_PREF_KEY = "MasterVolume";

    private void Start()
    {
        LoadSettings();
    }

    public void OnVolumeChanged(float value)
    {
        // En una implementación real, esto debería conectarse a un AudioMixer.
        // Ejemplo: audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VOLUME_PREF_KEY, value);
    }

    public void OnBackClicked()
    {
        // Guardar preferencias antes de salir
        PlayerPrefs.Save();
        
        if (mainMenuReference != null)
        {
            mainMenuReference.ShowMainMenu();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(VOLUME_PREF_KEY))
        {
            float savedVolume = PlayerPrefs.GetFloat(VOLUME_PREF_KEY);
            if (volumeSlider != null)
            {
                volumeSlider.value = savedVolume;
            }
            AudioListener.volume = savedVolume;
        }
        else
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = 1f;
            }
        }
    }
}
