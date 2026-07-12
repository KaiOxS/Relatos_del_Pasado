using UnityEngine;

public class ThrowSystem : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject projectilePrefab;
    public Transform throwPoint;
    private GameObject[] trajectoryDots;
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

        // Crear los puntos para la trayectoria (línea punteada)
        trajectoryDots = new GameObject[trajectorySegments];
        Material dotMat = new Material(Shader.Find("Sprites/Default"));
        dotMat.color = new Color(1f, 1f, 1f, 0.6f); // Blanco semi-transparente

        for (int i = 0; i < trajectorySegments; i++)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.transform.localScale = Vector3.one * 0.15f;
            Destroy(dot.GetComponent<Collider>()); // Quitamos colisiones
            dot.GetComponent<MeshRenderer>().material = dotMat;
            dot.SetActive(false);
            trajectoryDots[i] = dot;
        }
    }

    private void Start()
    {
        OnStonesChanged?.Invoke(GetThrowableCount());
    }

    private int GetThrowableCount()
    {
        if (InventorySystem.Instance == null) return 0;
        int count = 0;
        if (InventorySystem.Instance.items.ContainsKey("Piedra")) count += InventorySystem.Instance.items["Piedra"];
        if (InventorySystem.Instance.items.ContainsKey("BotellaVidrio")) count += InventorySystem.Instance.items["BotellaVidrio"];
        if (InventorySystem.Instance.items.ContainsKey("PedazoVidrio")) count += InventorySystem.Instance.items["PedazoVidrio"];
        return count;
    }

    private void Update()
    {
        int count = GetThrowableCount();
        if (count > 0)
        {
            if (input.AimHeld)
            {
                DrawTrajectory();
            }
            else 
            {
                HideTrajectory();
                if (input.ThrowTriggered)
                {
                    ThrowObject();
                    input.ConsumeThrow();
                }
            }
        }
        else
        {
            // Si no tenemos piedras, no mostramos trayectoria ni permitimos lanzar
            HideTrajectory();
            if (input.ThrowTriggered) input.ConsumeThrow(); // Consumir input de todos modos
        }
    }

    private void HideTrajectory()
    {
        if (trajectoryDots == null) return;
        for (int i = 0; i < trajectoryDots.Length; i++)
        {
            if (trajectoryDots[i] != null && trajectoryDots[i].activeSelf)
            {
                trajectoryDots[i].SetActive(false);
            }
        }
    }

    public void AddStones(int amount)
    {
        // Redirigir al sistema de inventario real (solo usado por legacy)
        if (InventorySystem.Instance != null) InventorySystem.Instance.AddItem("Piedra", amount);
        OnStonesChanged?.Invoke(GetThrowableCount());
    }

    private Vector3 GetThrowDirection()
    {
        // Si hay ratón, apuntamos hacia él para mayor precisión según el GDD
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            if (Camera.main != null)
            {
                // Calcular intersección del rayo del mouse con el plano 2.5D del jugador
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                Plane zPlane = new Plane(Vector3.forward, new Vector3(0, 0, transform.position.z));
                if (zPlane.Raycast(ray, out float enter))
                {
                    Vector3 worldMousePos = ray.GetPoint(enter);
                    Vector3 dir = (worldMousePos - throwPoint.position).normalized;
                    dir.z = 0; // Fijo en 2.5D
                    return dir;
                }
            }
        }
        
        // Fallback por defecto si no hay mouse
        float directionX = (player != null && !player.facingRight) ? -1f : 1f;
        return new Vector3(directionX, 0.5f, 0f).normalized;
    }

    private void DrawTrajectory()
    {
        if (throwPoint == null || trajectoryDots == null) return;
        
        Vector3 throwDirection = GetThrowDirection();
        Vector3 initialVelocity = throwDirection * throwForce;
        
        float timeStep = 0.1f;
        for (int i = 0; i < trajectorySegments; i++)
        {
            float timeOffset = timeStep * i;
            // Fórmula cinemática: p = p0 + v0*t + 0.5*g*t^2
            Vector3 progress = throwPoint.position + initialVelocity * timeOffset + Physics.gravity * 0.5f * timeOffset * timeOffset;
            
            trajectoryDots[i].transform.position = progress;
            trajectoryDots[i].SetActive(true);
        }
    }

    private void ThrowObject()
    {
        if (projectilePrefab == null || throwPoint == null || InventorySystem.Instance == null) return;
        
        // Consumir del inventario
        if (InventorySystem.Instance.HasItem("Piedra")) InventorySystem.Instance.UseItem("Piedra");
        else if (InventorySystem.Instance.HasItem("BotellaVidrio")) InventorySystem.Instance.UseItem("BotellaVidrio");
        else if (InventorySystem.Instance.HasItem("PedazoVidrio")) InventorySystem.Instance.UseItem("PedazoVidrio");

        OnStonesChanged?.Invoke(GetThrowableCount());

        GameObject proj = Instantiate(projectilePrefab, throwPoint.position, Quaternion.identity);
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            Vector3 throwDirection = GetThrowDirection();
            rb.linearVelocity = throwDirection * throwForce;
        }
    }
}
