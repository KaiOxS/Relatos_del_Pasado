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
    
    private Vector3 velocity = Vector3.zero;
    private PlayerController player;
    private Vector3 currentLookAhead;

    private void Start()
    {
        if (target != null)
        {
            player = target.GetComponent<PlayerController>();
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
        
        // Mantener el seguimiento suave
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
