using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Configuración Principal")]
    [Tooltip("La cámara principal que el fondo debe seguir.")]
    public Camera cam;

    [Tooltip("0 = El fondo se queda fijo en la pantalla. 1 = El fondo se queda fijo en el mundo (no hay parallax). Usa valores entre 0.1 y 0.9 para el efecto de profundidad.")]
    [Range(0f, 1f)]
    public float parallaxEffect = 0.1f;

    [Tooltip("¿Quieres que el fondo también tenga efecto parallax en el eje Y (arriba/abajo)?")]
    public bool parallaxEnY = false;

    private float startPosX;
    private float startPosY;

    void Start()
    {
        // Si no se asigna la cámara, busca la principal automáticamente
        if (cam == null) cam = Camera.main;

        startPosX = transform.position.x;
        startPosY = transform.position.y;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // Calcular la distancia que la cámara se ha movido desde el inicio
        float distX = cam.transform.position.x * parallaxEffect;
        float distY = cam.transform.position.y * parallaxEffect;

        // Calcular la nueva posición del fondo
        float newX = startPosX + distX;
        float newY = parallaxEnY ? startPosY + distY : transform.position.y;

        // Aplicar la nueva posición
        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}
