using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(StaminaSystem))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private InputManager input;
    private StaminaSystem staminaSystem;

    [Header("Movimiento Base")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 7f;
    public float exhaustedSpeed = 1.5f;
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
    private int ledgeDirection; // 1 = muro a la derecha, -1 = muro a la izquierda

    [Header("Configuración 2.5D")]
    [Tooltip("El personaje se mantendrá estricto en este valor Z")]
    public float fixedZPosition = 0f;

    [Header("Visuales (2D Sprite)")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public bool facingRight { get; private set; } = true;

    private Vector3 velocity;
    private bool isGrounded;

    // Máquina de estados modular
    public enum PlayerState { Idle, Walk, Sprint, Exhausted, Airborne, Sliding, WallSliding, Crouch, LedgeGrabbing }
    public PlayerState currentState;

    [System.Obsolete]
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = FindObjectOfType<InputManager>();
        staminaSystem = GetComponent<StaminaSystem>();

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
        if (slideCooldownTimer > 0) slideCooldownTimer -= Time.deltaTime;

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

        // Asegurar que el sprite esté ligeramente al frente para evitar Z-fighting con el cubo
        spriteRenderer.transform.localPosition = new Vector3(0, 0, -0.1f);
        
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
                case PlayerState.Sliding:
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
        // Posicionar la esfera exactamente en la base del CharacterController
        // para asegurar que sobrepasa un poco el borde y toca el suelo real
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - (controller.height / 2f), transform.position.z);
        Collider[] colliders = Physics.OverlapSphere(spherePosition, controller.radius * 0.9f, groundLayer, QueryTriggerInteraction.Ignore);
        
        isGrounded = false;
        foreach (Collider col in colliders)
        {
            if (col.gameObject != gameObject)
            {
                isGrounded = true;
                break;
            }
        }

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void CheckWall()
    {
        isWallSliding = false;
        wallDirection = 0;

        // No chequear paredes si ya está en el suelo o agarrado a un borde
        if (isGrounded || isGrabbingLedge) return;

        // Raycast para detectar muros a los lados (usa groundLayer para excluir al jugador)
        bool wallRight = Physics.Raycast(transform.position, Vector3.right, wallDistance, groundLayer);
        bool wallLeft = Physics.Raycast(transform.position, Vector3.left, wallDistance, groundLayer);

        if (wallRight || wallLeft)
        {
            wallDirection = wallRight ? 1 : -1;

            // Si el jugador aprieta hacia la pared mientras cae, se desliza
            if (velocity.y < 0 && input.MoveInput.x == wallDirection)
            {
                isWallSliding = true;
            }
        }
    }

    private void CheckLedge()
    {
        // Si ya está en el suelo o en slide, no detectamos ledge
        if (isGrounded || isSliding) return;

        // Solo buscar ledge cuando el jugador está subiendo o en el pico del salto
        // y no está ya agarrado
        if (isGrabbingLedge) return;

        // La detección se hace en la dirección que el jugador mira
        int checkDir = facingRight ? 1 : -1;
        Vector3 rayOrigin = transform.position; // Desde el centro del personaje

        // Raycast 1: Horizontal a la altura del centro/pecho – detecta si hay una pared al lado
        bool wallHit = Physics.Raycast(rayOrigin, Vector3.right * checkDir, ledgeCheckDistance, groundLayer);

        // Raycast 2: Horizontal a la altura de la cabeza – no debe detectar pared si estamos en el borde superior
        Vector3 headOrigin = rayOrigin + new Vector3(0, ledgeCheckHeight, 0);
        bool headHit = Physics.Raycast(headOrigin, Vector3.right * checkDir, ledgeCheckDistance, groundLayer);

        // Si el pecho choca pero la cabeza no, estamos exactamente en el borde superior
        if (wallHit && !headHit)
        {
            // Solo agarrar si el jugador va hacia esa dirección o está cayendo
            float inputDir = input.MoveInput.x;
            bool movingTowardWall = (checkDir == 1 && inputDir > 0.1f) || (checkDir == -1 && inputDir < -0.1f);

            if (movingTowardWall || velocity.y < 1f)
            {
                isGrabbingLedge = true;
                ledgeDirection = checkDir;
                velocity = Vector3.zero; // Detener todo movimiento
            }
        }
    }

    private void HandleLedgeGrab()
    {
        // Fijar el estado y detener movimiento
        currentState = PlayerState.LedgeGrabbing;
        velocity = Vector3.zero;

        // Si el jugador suelta la dirección contraria al muro o presiona abajo, suelta el borde
        bool releasingLedge = (ledgeDirection == 1 && input.MoveInput.x < -0.1f) ||
                              (ledgeDirection == -1 && input.MoveInput.x > 0.1f) ||
                              input.CrouchHeld;

        if (releasingLedge)
        {
            isGrabbingLedge = false;
            return;
        }

        // Si el jugador presiona Jump, TREPA el borde
        if (jumpBufferTimer > 0f)
        {
            isGrabbingLedge = false;
            // Impulso hacia arriba y hacia el lado opuesto al muro para quedar encima
            velocity.y = ledgeClimbForceY;
            velocity.x = ledgeDirection * ledgeClimbForceX;
            jumpBufferTimer = 0f;
        }
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
            if (input.CrouchHeld && isGrounded && !isSliding)
            {
                if (!isCrouching) StartCrouch();
                
                currentSpeed = 0f; // No se mueve agachado
                currentState = PlayerState.Crouch;
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
            else if (isWallSliding || (wallDirection != 0 && !isGrounded && !isGrabbingLedge))
            {
                PerformWallJump();
            }
        }
    }

    private void PerformJump()
    {
        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void PerformWallJump()
    {
        if (staminaSystem.HasEnoughStamina(staminaSystem.wallJumpDrain))
        {
            staminaSystem.ConsumeStamina(staminaSystem.wallJumpDrain);
            velocity.y = wallJumpForceY;
            velocity.x = -wallDirection * wallJumpForceX; // Empuje separándose del muro
            jumpBufferTimer = 0f;
            isWallSliding = false;
        }
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideCooldown; // Reiniciar cooldown
        currentState = PlayerState.Sliding;

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

        // El personaje se desliza estrictamente en el eje X
        controller.Move(new Vector3(slideDirectionX, 0, 0) * (slideSpeed * Time.deltaTime));

        if (slideTimer <= 0f)
        {
            isSliding = false;
            // Restaurar altura y centro originales
            controller.height = originalHeight;
            controller.center = originalCenter;
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("enemyAttack"))
        {
            RestartGame();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("enemyAttack"))
        {
            RestartGame();
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
