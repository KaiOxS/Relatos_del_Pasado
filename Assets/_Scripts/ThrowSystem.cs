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

    private PlayerController player;

    [Header("Inventario")]
    public int stoneCount = 0;
    public System.Action<int> OnStonesChanged;

    private void Awake()
    {
        input = FindObjectOfType<InputManager>();
        player = GetComponent<PlayerController>();
        if (trajectoryLine != null) trajectoryLine.enabled = false;
    }

    private void Start()
    {
        OnStonesChanged?.Invoke(stoneCount);
    }

    private void Update()
    {
        if (stoneCount > 0)
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
        else
        {
            // Si no tenemos piedras, no mostramos trayectoria ni permitimos lanzar
            if (trajectoryLine != null) trajectoryLine.enabled = false;
            if (input.ThrowTriggered) input.ConsumeThrow(); // Consumir input de todos modos
        }
    }

    public void AddStones(int amount)
    {
        stoneCount += amount;
        OnStonesChanged?.Invoke(stoneCount);
    }

    private Vector3 GetThrowDirection()
    {
        float directionX = (player != null && !player.facingRight) ? -1f : 1f;
        return new Vector3(directionX, 0.5f, 0f).normalized;
    }

    private void DrawTrajectory()
    {
        if (trajectoryLine == null || throwPoint == null) return;
        
        trajectoryLine.enabled = true;
        trajectoryLine.positionCount = trajectorySegments;
        
        // Dirección basada en hacia dónde mira el jugador
        Vector3 throwDirection = GetThrowDirection();
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
        
        stoneCount--;
        OnStonesChanged?.Invoke(stoneCount);

        GameObject proj = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            Vector3 throwDirection = GetThrowDirection();
            rb.linearVelocity = throwDirection * throwForce;
        }
    }
}
