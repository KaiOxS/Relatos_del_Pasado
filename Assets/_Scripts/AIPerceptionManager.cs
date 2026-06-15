using UnityEngine;
using System;

public class AIPerceptionManager : MonoBehaviour
{
    // Patrón Singleton especificado en el GDD para notificar ruidos globales
    public static AIPerceptionManager Instance { get; private set; }

    // Evento que los enemigos escucharán. Pasa la Posición del ruido y la Intensidad
    public Action<Vector3, float> OnNoiseHeard; 

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

    public void ReportNoise(Vector3 position, float intensity)
    {
        // Se llama desde el ThrowableObject al impactar
        OnNoiseHeard?.Invoke(position, intensity);
    }
}
