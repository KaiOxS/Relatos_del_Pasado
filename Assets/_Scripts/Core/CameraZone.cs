using UnityEngine;

public class CameraZone : MonoBehaviour
{
    [Header("Límites de la Cámara para esta Zona")]
    [Tooltip("El límite inferior e izquierdo (X mínimo, Y mínimo)")]
    public Vector2 minBounds;
    
    [Tooltip("El límite superior y derecho (X máximo, Y máximo)")]
    public Vector2 maxBounds;

    private void OnTriggerEnter(Collider other)
    {
        // Cuando José entra en esta zona, la cámara actualiza sus límites
        if (other.CompareTag("Player"))
        {
            CameraController cam = FindAnyObjectByType<CameraController>();
            if (cam != null)
            {
                cam.useBounds = true;
                cam.minBounds = this.minBounds;
                cam.maxBounds = this.maxBounds;
            }
        }
    }
    
    // (Opcional) Dibujar un rectángulo en la escena para que puedas ver los límites visualmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0f);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 1f);
        Gizmos.DrawWireCube(center, size);
    }
}
