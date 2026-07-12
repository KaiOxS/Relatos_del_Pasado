using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaSystem))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private InputManager input;
    private StaminaSystem staminaSystem;

    public AudioSource audioSource;

    public AudioClip hurtClip;



    [Header("Movimiento Base")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 7f;
    public float exhaustedSpeed = 1.5f;
    public float crouchSpeed = 1.5f; // Velocidad al moverse agachado
    public float rotationSpeed = 15f;

    [Header("Salto y Gravedad (GDD)")]
    public float jumpHeight = 1.5f;
    public float gravity = -40f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 3f;

    [Header("Detección de Suelo")]
    [Tooltip("Capas que se consideran suelo. Excluir la capa del jugador.")]
    public LayerMask groundLayer = ~0;

    [Header("Tolerancia Táctil (Asistencias)")]
    public float coyoteTime = 0.15f;
    private float coyoteTimer;
    private float wallCoyoteTimer;
    private int lastWallDirection;
    public float jumpBufferTime = 0.15f;
    private float jumpBufferTimer;

    [Header("Deslizamiento (Slide)")]
    public float slideSpeed = 8f;
    public float slideDuration = 0.5f;
    public float slideCooldown = 1f; // Nuevo: Tiempo de espera entre slides
    private float slideTimer;
    private float slideCooldownTimer; // Nuevo: Temporizador
    private bool isSliding;
    private float originalHeight;
    private Vector3 originalCenter;
    public float slideHeight = 1f;

    [Header("Agacharse (Crouch)")]
    public float crouchHeight = 1f;
    private bool isCrouching;

    [Header("Salto de Pared (Wall Jump)")]
    public float wallDistance = 0.6f;
    public float wallJumpForceX = 8f;
    public float wallJumpForceY = 6f;
    public float wallSlideSpeed = 2f;
    private bool isWallSliding;
    private int wallDirection; // 1 = Derecha, -1 = Izquierda

    [Header("Agarre de Borde (Ledge Grab)")]
    [Tooltip("Distancia horizontal del raycast para detectar el muro lateral.")]
    public float ledgeCheckDistance = 0.65f;
    [Tooltip("Distancia vertical del raycast para confirmar el borde libre encima.")]
    public float ledgeCheckHeight = 0.5f;
    [Tooltip("Fuerza vertical al trepar el borde.")]
    public float ledgeClimbForceY = 8f;
    [Tooltip("Fuerza horizontal al trepar el borde (salir del muro).")]
    public float ledgeClimbForceX = 4f;
    private bool isGrabbingLedge;
    private bool isClimbingLedge;
    private float ledgeCooldownTimer;
    private int ledgeDirection; // 1 = muro a la derecha, -1 = muro a la izquierda

    [Header("Configuración 2.5D")]
    [Tooltip("El personaje se mantendrá estricto en este valor Z")]
    public float fixedZPosition = 0f;

    [Header("Visuales (2D Sprite)")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public bool facingRight { get; private set; } = true;
    [Tooltip("Ajuste vertical del sprite para alinear los pies con el suelo")]
    public float visualOffsetY = 0.5f;

    private Vector3 velocity;
    private bool isGrounded;

    // Máquina de estados modular
    public enum PlayerState { Idle, Walk, Sprint, Exhausted, Airborne, Sliding, WallSliding, Crouch, LedgeGrabbing, CrouchWalk, Rolling, Hurt }
    public PlayerState currentState;

    private float damageTimer = 0f;

    public void TriggerDamageAnimation()
    {
        damageTimer = 0.4f; // Muestra la animación de daño sin inmovilizarte
    }

    [System.Obsolete]
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        // Seguro vital: Si el prefab se guardó con el controlador apagado por error (ej. durante un fundido a negro), forzar su encendido.
        if (controller != null) controller.enabled = true;

        staminaSystem = GetComponent<StaminaSystem>();
        input = FindObjectOfType<InputManager>();

        if (input == null) Debug.LogError("No se encontró InputManager en la escena.");
        originalHeight = controller.height;
        originalCenter = controller.center;

        // Buscar componentes visuales en los hijos
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Si tenemos un sprite, ocultamos el cubo base para que no se superpongan
        MeshRenderer cubeMesh = GetComponent<MeshRenderer>();
        if (cubeMesh != null && spriteRenderer != null)
        {
            cubeMesh.enabled = false;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return; // Detener todo movimiento si el juego está pausado

        if (slideCooldownTimer > 0) slideCooldownTimer -= Time.deltaTime;
        if (ledgeCooldownTimer > 0) ledgeCooldownTimer -= Time.deltaTime;
        if (damageTimer > 0) damageTimer -= Time.deltaTime;

        CheckGrounded();
        CheckWall();
        CheckLedge();
        HandleJumpBuffer();

        if (isGrabbingLedge)
        {
            HandleLedgeGrab();
        }
        else if (isSliding)
        {
            HandleSlide();
        }
        else
        {
            HandleStateAndMovement();
        }

        // La gravedad no se aplica mientras se agarra el borde
        if (!isGrabbingLedge)
        {
            ApplyGravity();
        }

        Enforce25D();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Asegurar que el sprite esté ligeramente al frente para evitar Z-fighting con el cubo,
        // pero aplicando un offset vertical (visualOffsetY) para alinear los pies.
        Vector3 localPos = spriteRenderer.transform.localPosition;
        spriteRenderer.transform.localPosition = new Vector3(localPos.x, visualOffsetY, -0.1f);
        
        // Forzar que el objeto del sprite siempre mire hacia adelante (plano 2.5D)
        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, 0);

        // Si tenemos un Animator, le pasamos el estado a través del parámetro "state"
        if (animator != null)
        {
            int animatorState = 0; // Default Idle
            switch (currentState)
            {
                case PlayerState.Idle:
                    animatorState = 0; // Idle
                    break;
                case PlayerState.Exhausted:
                    // Si está exhausto pero se mueve, usa walk(1). Si está quieto, usa idle(0).
                    animatorState = Mathf.Abs(input.MoveInput.x) > 0.1f ? 1 : 0;
                    break;
                case PlayerState.Walk:
                    animatorState = 1; // Walk
                    break;
                case PlayerState.Sprint:
                    animatorState = 2; // Run
                    break;
                case PlayerState.Airborne:
                case PlayerState.WallSliding:
                    animatorState = 3; // Jump
                    break;
                case PlayerState.Crouch:
                    animatorState = 4; // Crouch
                    break;
                case PlayerState.LedgeGrabbing:
                    animatorState = 5; // Animación de escalar / agarrarse al borde
                    break;
                case PlayerState.CrouchWalk:
                    animatorState = 6; // Animación de caminar agachado
                    break;
                case PlayerState.Rolling:
                    animatorState = 8; // Animación de rodar (Restaurado a 8)
                    break;
                case PlayerState.Hurt:
                    animatorState = 7; // Animación de daño (Restaurado a 7)
                    break;
            }

            // Forzar visual de daño si recibimos el trigger desde PlayerHealth
            if (damageTimer > 0f)
            {
                animatorState = 7;
            }

            animator.SetInteger("state", animatorState);
        }
    
        // Voltear el sprite según la dirección del movimiento en X
        if (input.MoveInput.x > 0.1f)
        {
            spriteRenderer.flipX = false;
            facingRight = true;
        }
        else if (input.MoveInput.x < -0.1f)
        {
            spriteRenderer.flipX = true;
            facingRight = false;
        }
    }

    private void CheckGrounded()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, controller.bounds.min.y, transform.position.z);
        Collider[] colliders = Physics.OverlapSphere(spherePosition, controller.radius * 0.9f, groundLayer, QueryTriggerInteraction.Ignore);
        
        bool hitGround = false;
        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                hitGround = true;
                break;
            }
        }

        if (hitGround && velocity.y <= 0.1f)
        {
            isGrounded = true;
            coyoteTimer = coyoteTime;
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        else
        {
            isGrounded = false;
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void CheckWall()
    {
        isWallSliding = false;
        wallDirection = 0;

        if (isGrounded || isGrabbingLedge) return;

        bool wallRight = Physics.Raycast(transform.position, Vector3.right, wallDistance, groundLayer);
        bool wallLeft = Physics.Raycast(transform.position, Vector3.left, wallDistance, groundLayer);

        if (wallRight || wallLeft)
        {
            wallDirection = wallRight ? 1 : -1;
            lastWallDirection = wallDirection;
            wallCoyoteTimer = coyoteTime;

            if (velocity.y < 0 && input.MoveInput.x == wallDirection)
            {
                isWallSliding = true;
            }
        }
        else
        {
            wallCoyoteTimer -= Time.deltaTime;
        }
    }

    private void CheckLedge()
    {
        if (isGrounded || isSliding || isGrabbingLedge || ledgeCooldownTimer > 0f) return;
        if (input.MoveInput.y < -0.1f) return;

        int checkDir = facingRight ? 1 : -1;
        float topY = controller.bounds.max.y;
        
        Vector3 headOrigin = new Vector3(transform.position.x, topY, transform.position.z);
        Vector3 handsOrigin = new Vector3(transform.position.x, topY - ledgeCheckHeight, transform.position.z);

        bool headHit = Physics.Raycast(headOrigin, Vector3.right * checkDir, ledgeCheckDistance, groundLayer);
        bool handsHit = Physics.Raycast(handsOrigin, Vector3.right * checkDir, ledgeCheckDistance, groundLayer);
        bool aboveHeadHit = Physics.Raycast(headOrigin + Vector3.up * 0.2f, Vector3.right * checkDir, ledgeCheckDistance, groundLayer);

        if (handsHit && !headHit && !aboveHeadHit)
        {
            float inputDir = input.MoveInput.x;
            bool movingTowardWall = (checkDir == 1 && inputDir > 0.1f) || (checkDir == -1 && inputDir < -0.1f);

            if (movingTowardWall || velocity.y < 1f)
            {
                // Magnetismo: Ajustar la altura para que el agarre sea preciso al píxel
                Vector3 rayStart = new Vector3(transform.position.x + (checkDir * ledgeCheckDistance * 0.8f), topY + 0.5f, transform.position.z);
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit ledgeHit, ledgeCheckHeight + 1.0f, groundLayer))
                {
                    float ledgeY = ledgeHit.point.y;
                    float handsCurrentY = topY - ledgeCheckHeight;
                    float offsetY = ledgeY - handsCurrentY;
                    
                    if (Mathf.Abs(offsetY) < 0.8f) // Tolerancia máxima
                    {
                        controller.enabled = false;
                        transform.position += new Vector3(0, offsetY, 0);
                        controller.enabled = true;
                    }
                }

                isGrabbingLedge = true;
                ledgeDirection = checkDir;
                velocity = Vector3.zero;
            }
        }
    }

    private void HandleLedgeGrab()
    {
        if (isClimbingLedge) return;

        // Fijar el estado y detener movimiento
        currentState = PlayerState.LedgeGrabbing;
        velocity = Vector3.zero;

        // Si el jugador suelta la dirección contraria al muro o presiona abajo (S), suelta el borde
        bool releasingLedge = (ledgeDirection == 1 && input.MoveInput.x < -0.1f) ||
                              (ledgeDirection == -1 && input.MoveInput.x > 0.1f) ||
                              input.CrouchHeld || 
                              input.MoveInput.y < -0.1f;

        if (releasingLedge)
        {
            isGrabbingLedge = false;
            ledgeCooldownTimer = 0.5f; // Evitar que se vuelva a agarrar al instante al caer
            return;
        }

        // Si el jugador presiona Jump, TREPA el borde
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer = 0f;
            StartCoroutine(LedgeClimbRoutine());
        }
    }

    private System.Collections.IEnumerator LedgeClimbRoutine()
    {
        isClimbingLedge = true;
        
        // Tiempo que tarda en realizar la animación visual de trepar (ajustable)
        float climbDuration = 0.35f; 
        float elapsed = 0f;

        Vector3 startPos = transform.position;
        float distanceToLedge = (controller.bounds.max.y - ledgeCheckHeight) - controller.bounds.min.y;
        Vector3 targetPos = startPos + new Vector3(ledgeDirection * (controller.radius + 0.6f), distanceToLedge + 0.3f, 0);

        // Apagamos el CharacterController para que no pelee con nuestro movimiento matemático
        controller.enabled = false;

        // Movemos el personaje suavemente de un punto a otro mientras la animación se reproduce
        while (elapsed < climbDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, (elapsed / climbDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Aseguramos que termine exactamente en el destino
        transform.position = targetPos;
        controller.enabled = true;
        
        // Devolvemos el control al jugador
        isGrabbingLedge = false;
        isClimbingLedge = false;
        velocity = Vector3.zero;
    }

    private void HandleJumpBuffer()
    {
        if (input.JumpTriggered)
        {
            jumpBufferTimer = jumpBufferTime;
            input.ConsumeJump();
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private float slideDirectionX;

    private void HandleStateAndMovement()
    {
        float currentSpeed = walkSpeed;
        currentState = PlayerState.Idle;

        // Movimiento puramente bidimensional (Eje X)
        Vector3 moveDirection = new Vector3(input.MoveInput.x, 0f, 0f);

        // --- MANEJO DE ESTADOS Y ESTAMINA ---
        if (staminaSystem.IsExhausted)
        {
            currentSpeed = exhaustedSpeed;
            currentState = PlayerState.Exhausted;
            input.ConsumeSlide(); // No puede deslizarse si está exhausto
        }
        else
        {
            // Intentar Slide
            if (input.SlideTriggered)
            {
                // Con tal de que se esté moviendo y esté en el suelo, permitimos el slide
                if (isGrounded && moveDirection.magnitude > 0.1f && staminaSystem.HasEnoughStamina(staminaSystem.slideDrain) && slideCooldownTimer <= 0)
                {
                    StartSlide();
                    return; // Saltamos este frame de movimiento para procesar el Slide
                }
                // Si presionó el botón pero no cumple las condiciones, consumimos el input para que no se guarde
                input.ConsumeSlide();
            }

            // Agacharse
            bool wantsToCrouch = input.CrouchHeld;

            // Si estaba agachado y quiere soltar la tecla, primero revisamos si hay espacio arriba
            if (isCrouching && !wantsToCrouch)
            {
                if (!CanStandUp())
                {
                    wantsToCrouch = true; // Lo obligamos a seguir agachado porque hay techo
                }
            }

            if (wantsToCrouch && isGrounded && !isSliding)
            {
                if (!isCrouching) StartCrouch();
                
                // Si el jugador se mueve, aplicamos la velocidad de agachado y estado CrouchWalk
                if (moveDirection.magnitude > 0.1f)
                {
                    currentSpeed = crouchSpeed;
                    currentState = PlayerState.CrouchWalk;
                }
                else
                {
                    currentSpeed = 0f;
                    currentState = PlayerState.Crouch;
                }
            }
            else
            {
                if (isCrouching) StopCrouch();

                // Movimiento normal
                if (moveDirection.magnitude > 0.1f)
                {
                    if (input.SprintHeld && staminaSystem.HasEnoughStamina(staminaSystem.sprintDrainRate * Time.deltaTime))
                    {
                        currentSpeed = sprintSpeed;
                        currentState = PlayerState.Sprint;
                        staminaSystem.ConsumeStaminaOverTime(staminaSystem.sprintDrainRate);
                    }
                    else
                    {
                        currentSpeed = walkSpeed;
                        currentState = PlayerState.Walk;
                    }
                }
            }
        }

        if (!isGrounded)
        {
            currentState = isWallSliding ? PlayerState.WallSliding : PlayerState.Airborne;
        }

        // --- APLICAR MOVIMIENTO ---
        controller.Move(moveDirection * (currentSpeed * Time.deltaTime));

        // --- CONDICIÓN DE SALTO ---
        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                PerformJump();
            }
            else if (isWallSliding || (wallDirection != 0 && !isGrounded && !isGrabbingLedge) || wallCoyoteTimer > 0f)
            {
                int jumpDir = (wallDirection != 0) ? wallDirection : lastWallDirection;
                PerformWallJump(jumpDir);
            }
        }
    }

    private void PerformJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void PerformWallJump(int jumpDir)
    {
        if (staminaSystem.HasEnoughStamina(staminaSystem.wallJumpDrain))
        {
            staminaSystem.ConsumeStamina(staminaSystem.wallJumpDrain);
            velocity.y = wallJumpForceY;
            velocity.x = -jumpDir * wallJumpForceX;
            jumpBufferTimer = 0f;
            wallCoyoteTimer = 0f;
            isWallSliding = false;
        }
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown; // Reiniciar cooldown
        currentState = PlayerState.Rolling;

        // Guardar la dirección del slide (X)
        slideDirectionX = input.MoveInput.x > 0 ? 1f : -1f;

        // Reducir la altura y bajar el centro para que no flote ni atraviese el suelo
        controller.height = slideHeight;
        controller.center = new Vector3(originalCenter.x, originalCenter.y - (originalHeight - slideHeight) / 2f, originalCenter.z);

        staminaSystem.ConsumeStamina(staminaSystem.slideDrain);
        input.ConsumeSlide();
    }

    private void HandleSlide()
    {
        slideTimer -= Time.deltaTime;
        controller.Move(new Vector3(slideDirectionX, 0, 0) * (slideSpeed * Time.deltaTime));

        // Permitir saltar durante el deslizamiento para mayor fluidez
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            if (CanStandUp())
            {
                isSliding = false;
                controller.height = originalHeight;
                controller.center = originalCenter;
                PerformJump();
                return;
            }
        }

        if (slideTimer <= 0f)
        {
            isSliding = false;
            if (!CanStandUp())
            {
                StartCrouch();
            }
            else
            {
                controller.height = originalHeight;
                controller.center = originalCenter;
            }
        }
    }

    public void StartCrouch()
    {
        isCrouching = true;
        controller.height = crouchHeight;
        controller.center = new Vector3(originalCenter.x, originalCenter.y - (originalHeight - crouchHeight) / 2f, originalCenter.z);
    }

    public void StopCrouch()
    {
        isCrouching = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
    }

    private bool CanStandUp()
    {
        // La distancia que necesitamos subir para estar de pie
        float distance = originalHeight - controller.height;
        
        // Lanzamos una esfera invisible desde la cabeza actual hacia arriba
        // Usamos un radio un poco menor al del jugador para que no choque con paredes laterales
        Vector3 origin = new Vector3(transform.position.x, controller.bounds.max.y - (controller.radius * 0.9f), transform.position.z);
        
        // Si choca con algo de la capa GroundLayer, no podemos levantarnos
        return !Physics.SphereCast(origin, controller.radius * 0.9f, Vector3.up, out RaycastHit hit, distance + 0.1f, groundLayer);
    }

    private void ApplyGravity()
    {
        float currentGravity = gravity;

        if (isWallSliding)
        {
            // Resbalando por la pared (velocidad controlada)
            velocity.y = -wallSlideSpeed;
        }
        else
        {
            if (velocity.y < 0)
            {
                currentGravity *= fallMultiplier;
            }
            else if (velocity.y > 0 && !input.JumpHeld)
            {
                currentGravity *= lowJumpMultiplier;
            }

            // Decaimiento suave de la inercia en X después de un Wall Jump
            if (!isGrounded && velocity.x != 0)
            {
                velocity.x = Mathf.Lerp(velocity.x, 0, Time.deltaTime * 3f);
                controller.Move(new Vector3(velocity.x, 0, 0) * Time.deltaTime);
            }

            velocity.y += currentGravity * Time.deltaTime;
        }

        controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
    }

    private void Enforce25D()
    {
        if (Mathf.Abs(transform.position.z - fixedZPosition) > 0.01f)
        {
            Vector3 pos = transform.position;
            pos.z = fixedZPosition;
            transform.position = pos;
        }
    }
    /*private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("enemyAttack"))
        {
            audioSource.PlayOneShot(hurtClip);
            RestartGame();
        }
    }*/

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("enemyAttack"))
        {
            // Evitar que mate instantáneamente ya que PlayerHealth maneja la salud
            // RestartGame();
        }
    }

    private void RestartGame()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ShowGameOver();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
