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
    private bool isAttacking = false;
    private float initialZ;
    private Vector3 lastPosition;

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
            agent.isStopped = true;
            agent.speed = petrifiedSpeed;
            
            if (animator != null)
            {
                animator.SetInteger("state", 0); // 0 = Idle
            }
        }
        else if (isAttacking)
        {
            // Mientras ataca no se mueve
            agent.isStopped = true;
        }
        else
        {
            // Verificamos si está lo suficientemente cerca para atacar
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer <= attackRange)
            {
                StartCoroutine(AttackRoutine());
                return;
            }

            // Persiguiendo agresivamente
            agent.isStopped = false;
            agent.speed = stalkingSpeed;
            
            if (animator != null)
            {
                if (animator.GetInteger("state") != 2) animator.SetInteger("state", 2); // 2 = Run
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

    protected override void OnTriggerEnter(Collider other)
    {
        // Mariangula ya no hace daño por simple contacto físico.
        // El daño se maneja exclusivamente en su rutina de ataque.
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        audioSource.PlayOneShot(laughClip);
        isAttacking = true;
        agent.isStopped = true;

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
}
