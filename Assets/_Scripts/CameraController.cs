using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Tracking")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 3f, -15f);
    public float smoothTime = 0.25f;
    
    [Header("Look Ahead (2.5D)")]
    public float lookAheadDistance = 3f;
    public float lookAheadSpeed = 2f;
    
    [Header("Límites de Cámara (Topes)")]
    [Tooltip("Activa esta opción para evitar que la cámara salga del mapa")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    
    private Vector3 velocity = Vector3.zero;
    private PlayerController player;
    private Vector3 currentLookAhead;

    private void Start()
    {
        // Buscar automáticamente a José si olvidamos asignarlo en el Inspector de este nuevo nivel
        if (target == null)
        {
            PlayerController foundPlayer = FindAnyObjectByType<PlayerController>();
            if (foundPlayer != null)
            {
                target = foundPlayer.transform;
            }
        }

        if (target != null)
        {
            player = target.GetComponent<PlayerController>();
        }
        else
        {
            Debug.LogWarning("¡La cámara no tiene a quién seguir! Asegúrate de que José esté en la escena.");
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calcular el desplazamiento anticipado basado en hacia dónde mira el jugador
        if (player != null && player.currentState != PlayerController.PlayerState.Idle)
        {
            float directionX = player.facingRight ? 1f : -1f;
            Vector3 targetLookAhead = new Vector3(directionX, 0, 0) * lookAheadDistance;
            currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSpeed);
        }
        else
        {
            currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, Time.deltaTime * lookAheadSpeed);
        }

        Vector3 targetPosition = target.position + offset + currentLookAhead;
        
        // Aplicar los topes si están activados
        if (useBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }
        
        // Mantener el seguimiento suave
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    public void SnapToTarget()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            
            if (useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
            }
            
            transform.position = targetPosition;
            velocity = Vector3.zero; // Detener cualquier inercia
        }
    }
}
