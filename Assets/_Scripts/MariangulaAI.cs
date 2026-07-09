using UnityEngine;
using UnityEngine.AI;

public class MariangulaAI : BaseEnemyAI
{
    [Header("Mariangula Settings")]
    public float petrifiedSpeed = 0f;
    public float stalkingSpeed = 7.5f; // Aumentado para que sea un poco más rápida
    
    [Tooltip("Distancia a la que Mariangula intentará acercarse")]
    public float attackRange = 1.5f;

    [Header("Jumpscare Teleport")]
    public bool enableJumpscareTeleport = true;
    public float maxDistanceBeforeTeleport = 18f;
    [Tooltip("Distancia por delante del jugador a la que aparecerá para asustarlo")]
    public float teleportOffsetAhead = 8f;

    [Header("Animations")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Ajuste vertical del sprite para alinear los pies con el suelo")]
    public float visualOffsetY = 0f;

    private Camera mainCamera;
    private LightController playerLight;
    private bool isBeingWatched;
    private bool isAttacking = false;
    private float initialZ;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private float teleportCooldownTimer = 0f;

    public AudioSource audioSource;

    public AudioClip laughClip;


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
                // Ignorar colisiones con uno mismo y con el jugador
                if (!hit.collider.isTrigger && hit.collider.transform.root != transform.root && !hit.collider.CompareTag("Player"))
                {
                    agent.velocity = Vector3.zero;
                    Vector3 safePos = transform.position;
                    safePos.x = hit.point.x - (desiredDir.x * radius);
                    pos = safePos; 
                    
                    // Si se choca con un muro, aumentamos el tiempo atascada
                    stuckTimer += Time.deltaTime;
                }
                else
                {
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f; // No hay colisión, reiniciamos temporizador
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        if (teleportCooldownTimer > 0f)
        {
            teleportCooldownTimer -= Time.deltaTime;
        }

        // --- NUEVO: SISTEMA DE DESATASCO (Si no hay salida o choca con pared) ---
        if (enableJumpscareTeleport && !isAttacking && currentState != AIState.Stunned && teleportCooldownTimer <= 0f)
        {
            // Si la ruta no llega al jugador (ej. cuarto cerrado o abismo)
            bool hasNoPath = (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid);
            
            // Si no tiene ruta o si lleva chocando con un muro más de 0.15 segundos (casi inmediato)
            if (hasNoPath || stuckTimer > 0.15f)
            {
                stuckTimer = 0f;
                TriggerJumpscareTeleport();
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

    private float watchedGraceTimer = 0f;

    private void CheckIfWatched()
    {
        if (playerTransform == null) return;
        if (mainCamera == null) mainCamera = Camera.main ?? FindObjectOfType<Camera>();
        if (mainCamera == null) return;

        bool currentlyWatched = false;

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

            if (Physics.Raycast(rayStart, rayDir, out RaycastHit hit))
            {
                // Aquí estaba el bug: el collider del greybox está en un hijo, no en el gameObject raíz.
                if (hit.collider.transform.root == transform.root)
                {
                    currentlyWatched = true;
                }
            }
            else
            {
                // Si el raycast no golpeó nada asumimos que sí la estás viendo.
                currentlyWatched = true;
            }
        }

        // Sistema anti-parpadeo: si la acabamos de ver, mantenemos el estado un instante
        if (currentlyWatched)
        {
            isBeingWatched = true;
            watchedGraceTimer = 0.2f; // Mantenerla vista por 0.2 segundos extra
        }
        else
        {
            if (watchedGraceTimer > 0)
            {
                watchedGraceTimer -= Time.deltaTime;
                isBeingWatched = true;
            }
            else
            {
                isBeingWatched = false;
            }
        }
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
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.speed = petrifiedSpeed;
            }
            
            if (animator != null)
            {
                animator.SetInteger("state", 0); // 0 = Idle
            }
        }
        else if (isAttacking)
        {
            // Mientras ataca no se mueve
            if (agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        {
            // Verificamos si está lo suficientemente cerca para atacar
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // --- NUEVO: SISTEMA DE JUMPSCARE (Teletransporte si el jugador se aleja mucho) ---
            if (enableJumpscareTeleport && distToPlayer > maxDistanceBeforeTeleport && teleportCooldownTimer <= 0f)
            {
                TriggerJumpscareTeleport();
                return;
            }

            if (distToPlayer <= attackRange)
            {
                StartCoroutine(AttackRoutine());
                return;
            }

            // Persiguiendo agresivamente
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = stalkingSpeed;
            }
            
            if (animator != null)
            {
                if (animator.GetInteger("state") != 2) animator.SetInteger("state", 2); // 2 = Run
            }
            
            if (playerTransform != null && agent.isOnNavMesh)
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

    protected override void OnTriggerEnter(Collider other)
    {
        // Mariangula ya no hace daño por simple contacto físico.
        // El daño se maneja exclusivamente en su rutina de ataque.
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        if (audioSource != null && laughClip != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(laughClip);
        }
        isAttacking = true;
        if (agent.isOnNavMesh) agent.isStopped = true;

        // Disparamos la animación de ataque.
        // Asumo que el valor 3 será el estado de "Ataque" en el Animator (state = 3).
        if (animator != null) animator.SetInteger("state", 3);

        // Esperamos medio segundo hasta el punto de impacto de la animación (ajústalo si la animación es más rápida/lenta)
        yield return new WaitForSeconds(0.5f);

        // Si el jugador sigue en el rango y Mariangula no ha sido aturdida, le hace daño.
        if (currentState != AIState.Stunned && playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer <= attackRange + 0.5f) // Le damos un pequeño margen extra para que no falle injustamente
            {
                PlayerHealth health = playerTransform.GetComponentInParent<PlayerHealth>();
                if (health == null) health = playerTransform.GetComponent<PlayerHealth>();
                
                if (health != null)
                {
                    float dmg = health.maxHealth * damagePercent;
                    health.TakeDamage(dmg);
                }
            }
        }

        // Esperamos a que termine el resto de la animación
        yield return new WaitForSeconds(0.5f);

        // Volvemos a la normalidad
        isAttacking = false;
    }

    private void TriggerJumpscareTeleport()
    {
        if (playerTransform == null) return;

        // Intentamos obtener la dirección en la que mira el jugador (PlayerController asume rotación o Input)
        // En 2.5D, lo más seguro es usar el movimiento de cámara o la dirección del modelo
        float directionX = playerTransform.localScale.x > 0 ? 1f : -1f; 
        
        // Si tienes la rotación configurada, podríamos usar playerTransform.right o forward.
        // Pero para ser robustos en 2.5D, usamos el vector hacia donde mire el player.
        // Asumiendo que el PlayerController voltea el sprite invirtiendo la escala X o rotando en Y:
        if (playerTransform.forward.x > 0.1f) directionX = 1f;
        else if (playerTransform.forward.x < -0.1f) directionX = -1f;

        // Calculamos la nueva posición X: Justo delante del jugador
        Vector3 newPos = playerTransform.position;
        newPos.x += directionX * teleportOffsetAhead;
        newPos.y += 1f; // Lo subimos un poco por si el suelo es irregular
        newPos.z = initialZ;

        // Verificamos si la nueva posición está sobre el NavMesh (con un radio grande de 10 metros para buscar el suelo más cercano)
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(newPos, out navHit, 10f, NavMesh.AllAreas))
        {
            newPos = navHit.position; // Ajusta a la posición legal más cercana (suelo firme)
        }
        else
        {
            // PLAN B: Si no hay NavMesh en 10 metros a la redonda (es un precipicio absoluto o vacío),
            // la teletransportamos de forma segura exactamente donde está parado el jugador para evitar que caiga.
            newPos = playerTransform.position;
            newPos.z = initialZ;
            if (NavMesh.SamplePosition(newPos, out navHit, 5f, NavMesh.AllAreas))
            {
                newPos = navHit.position;
            }
        }

        // Teletransportamos al NavMeshAgent de manera limpia
        agent.Warp(newPos);
        transform.position = newPos;
        
        // Hacemos que mire inmediatamente al jugador
        Vector3 localPos = spriteRenderer.transform.localPosition;
        if (directionX > 0) spriteRenderer.flipX = true; // Si el jugador va a la derecha, Mariangula mira a la izquierda
        else spriteRenderer.flipX = false;
        
        // Reproducir sonido para el susto solo si no está sonando ya
        if (audioSource != null && laughClip != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(laughClip);
        }
        
        // Añadir cooldown para evitar spamming de teletransportes mientras el NavMesh se recalcula
        teleportCooldownTimer = 5f;
        
        Debug.Log("¡Jumpscare! Mariangula se teletransportó al frente del jugador.");
    }
}
