using UnityEngine;
using UnityEngine.AI;

public class MariangulaAI : BaseEnemyAI
{
    [Header("Mariangula Settings")]
    public float petrifiedSpeed = 0f;
    public float stalkingSpeed = 6f;
    
    [Tooltip("Distancia a la que Mariangula intentará acercarse")]
    public float attackRange = 1.5f;

    [Header("Animations")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Ajuste vertical del sprite para alinear los pies con el suelo")]
    public float visualOffsetY = 0f;

    private Camera mainCamera;
    private LightController playerLight;
    private bool isBeingWatched;
    private float initialZ;
    private Vector3 lastPosition;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        initialZ = transform.position.z;
        lastPosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();
        if (playerTransform != null)
        {
            playerLight = playerTransform.GetComponent<LightController>();
            if (playerLight != null)
            {
                playerLight.OnFlashlightToggled += HandleFlashlightToggle;
            }
        }
        
        agent.speed = stalkingSpeed;
        currentState = AIState.Chase; // Mariangula siempre está al acecho
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (playerLight != null)
        {
            playerLight.OnFlashlightToggled -= HandleFlashlightToggle;
        }
    }

    protected override void Update()
    {
        // Verificar si el jugador la está mirando
        CheckIfWatched();

        base.Update(); // Ejecuta la máquina de estados
        
        // Bloquear el eje Z para que no intente rodear obstáculos
        Vector3 pos = transform.position;
        pos.z = initialZ;
        
        // Prevenir que atraviese muros usando su radio y desiredVelocity
        Vector3 desiredDir = agent.desiredVelocity;
        desiredDir.y = 0; 
        desiredDir.z = 0; // Solo predecimos sobre X para ser estrictamente 2.5D
        
        if (desiredDir.sqrMagnitude > 0.01f)
        {
            desiredDir.Normalize();
            float radius = agent.radius > 0 ? agent.radius : 0.5f;
            
            // Lanzamos raycast desde el centro (asumiendo altura media de 1 metro)
            Vector3 origin = transform.position + Vector3.up * 1f;
            
            if (Physics.Raycast(origin, desiredDir, out RaycastHit hit, radius + 0.1f))
            {
                // Ignorar colisiones con uno mismo (incluso si el collider está en un hijo) y con el jugador
                if (!hit.collider.isTrigger && hit.collider.transform.root != transform.root && !hit.collider.CompareTag("Player"))
                {
                    // Nos estamos chocando con un muro. Detenemos la velocidad del agente.
                    agent.velocity = Vector3.zero;
                    
                    // Empujarla hacia atrás exactamente al borde del collider para que no quede trabada a la mitad
                    Vector3 safePos = transform.position;
                    safePos.x = hit.point.x - (desiredDir.x * radius);
                    pos = safePos; // Aplicamos a nuestra variable de posición que será forzada en Z
                }
            }
        }

        transform.position = pos;
        agent.nextPosition = pos; // Sincronizar internamente el NavMeshAgent
        lastPosition = pos;

        // Voltear el sprite según la dirección del movimiento en X, y aplicar offset vertical
        if (spriteRenderer != null)
        {
            Vector3 localPos = spriteRenderer.transform.localPosition;
            spriteRenderer.transform.localPosition = new Vector3(localPos.x, visualOffsetY, -0.1f);

            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                if (agent.velocity.x > 0.1f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (agent.velocity.x < -0.1f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }
    }

    private void CheckIfWatched()
    {
        if (playerTransform == null) return;
        if (mainCamera == null) mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCamera == null) return;

        // 1. ¿Está en la pantalla? (Con un margen de error del 20%)
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);
        bool inCameraView = viewportPoint.x >= -0.2f && viewportPoint.x <= 1.2f && viewportPoint.z > 0;

        // 2. ¿El jugador la está mirando? (Ignoramos la Y para que no importe si salta)
        Vector3 dirToEnemy = (transform.position - playerTransform.position);
        dirToEnemy.y = 0;
        dirToEnemy.Normalize();
        
        float lookDot = Vector3.Dot(playerTransform.forward, dirToEnemy);
        bool playerLookingAtMe = lookDot > 0f; // > 0 significa que al menos está en su hemisferio frontal

        // 3. ¿La linterna está encendida?
        bool flashlightOn = playerLight != null && playerLight.isFlashlightOn;

        if (inCameraView && playerLookingAtMe && flashlightOn)
        {
            // Tiramos un rayo desde UN POCO MÁS ADELANTE del jugador para no chocar con su propia cápsula
            Vector3 rayStart = playerTransform.position + Vector3.up + (dirToEnemy * 1.5f);
            Vector3 targetCenter = transform.position + Vector3.up; // Apuntar al pecho de Mariangula
            Vector3 rayDir = (targetCenter - rayStart).normalized;

            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDir, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isBeingWatched = true;
                    return;
                }
            }
            else
            {
                // Si el raycast no golpeó nada (tal vez olvidaste ponerle un collider a Mariangula),
                // asumimos que sí la estás viendo para que el prototipo funcione.
                isBeingWatched = true;
                return;
            }
        }

        isBeingWatched = false;
    }

    protected override void UpdatePatrol()
    {
        // Mariangula no patrulla de forma convencional, siempre persigue si no es vista.
        currentState = AIState.Chase;
    }

    protected override void UpdateChase()
    {
        if (isBeingWatched || currentState == AIState.Stunned)
        {
            // Petrificada
            agent.isStopped = true;
            agent.speed = petrifiedSpeed;
            
            if (animator != null)
            {
                animator.SetInteger("state", 0); // 0 = Idle
            }
        }
        else
        {
            // Persiguiendo agresivamente
            agent.isStopped = false;
            agent.speed = stalkingSpeed;
            
            if (animator != null)
            {
                animator.SetInteger("state", 2); // 2 = Run
            }
            
            if (playerTransform != null)
            {
                agent.SetDestination(new Vector3(playerTransform.position.x, playerTransform.position.y, initialZ));
            }
        }
    }

    private void HandleFlashlightToggle(bool isOn)
    {
        // Reevaluar inmediatamente si la linterna cambia
        CheckIfWatched();
    }
}
