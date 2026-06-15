using UnityEngine;

public class ThrowSystem : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject projectilePrefab;
    public Transform throwPoint;
    public LineRenderer trajectoryLine;
    private InputManager input;

    [Header("Configuración de Lanzamiento")]
    public float throwForce = 15f;
    public int trajectorySegments = 20;

    private void Awake()
    {
        input = FindObjectOfType<InputManager>();
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    private void Update()
    {
        if (input.AimHeld)
        {
            DrawTrajectory();
        }
        else if (input.ThrowTriggered)
        {
            ThrowObject();
            input.ConsumeThrow();
            if (trajectoryLine != null) trajectoryLine.enabled = false;
        }
    }

    private void DrawTrajectory()
    {
        if (trajectoryLine == null || throwPoint == null) return;
        
        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = trajectorySegments;
        
        // Dirección: Adelante y ligeramente hacia arriba
        Vector3 throwDirection = (transform.forward + Vector3.up * 0.5f).normalized;
        Vector3 initialVelocity = throwDirection * throwForce;
        
        float timeStep = 0.1f;
        for (int i = 0; i < trajectorySegments; i++)
        {
            float timeOffset = timeStep * i;
            // Fórmula cinemática: p = p0 + v0*t + 0.5*g*t^2
            Vector3 progress = throwPoint.position + initialVelocity * timeOffset + Physics.gravity * 0.5f * timeOffset * timeOffset;
            trajectoryLine.SetPosition(i, progress);
        }
    }

    private void ThrowObject()
    {
        if (projectilePrefab == null || throwPoint == null) return;
        
        GameObject proj = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            Vector3 throwDirection = (transform.forward + Vector3.up * 0.5f).normalized;
            rb.linearVelocity = throwDirection * throwForce;
        }
    }
}
