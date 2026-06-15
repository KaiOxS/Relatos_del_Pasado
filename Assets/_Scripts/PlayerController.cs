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
    public float jumpHeight = 2.5f;
    public float gravity = -20f;
    public float fallMultiplier = 1.5f;
    public float lowJumpMultiplier = 2f;

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

    [Header("Salto de Pared (Wall Jump)")]
    public float wallDistance = 0.6f;
    public float wallJumpForceX = 8f;
    public float wallJumpForceY = 6f;
    public float wallSlideSpeed = 2f;
    private bool isWallSliding;
    private int wallDirection; // 1 = Derecha, -1 = Izquierda

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
    public enum PlayerState { Idle, Walk, Sprint, Exhausted, Airborne, Sliding, WallSliding }
    public PlayerState currentState;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = FindObjectOfType<InputManager>();
        staminaSystem = GetComponent<StaminaSystem>();

        if (input == null) Debug.LogError("No se encontró InputManager en la escena.");
        originalHeight = controller.height;
        originalCenter = controller.center;

        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (slideCooldownTimer > 0) slideCooldownTimer -= Time.deltaTime;

        CheckGrounded();
        CheckWall();
        HandleJumpBuffer();

        if (isSliding)
        {
            HandleSlide();
        }
        else
        {
            HandleStateAndMovement();
        }

        ApplyGravity();
        Enforce25D();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Forzar que el objeto del sprite siempre mire hacia la cámara (Z-)
        // Esto evita que herede rotaciones o que se vea de lado
        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, 0);

        // Si tenemos un Animator, le pasamos el estado actual
        if (animator != null)
        {
            animator.SetInteger("state", (int)currentState);
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
        // Usar un SphereCast pequeño en la base del jugador para detectar el suelo
        // Esto es más fiable que controller.isGrounded en 2.5D
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - (controller.height / 2) + controller.radius, transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, controller.radius * 0.9f, ~0, QueryTriggerInteraction.Ignore);

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

        if (isGrounded) return;

        // Raycast para detectar muros a los lados
        bool wallRight = Physics.Raycast(transform.position, Vector3.right, wallDistance);
        bool wallLeft = Physics.Raycast(transform.position, Vector3.left, wallDistance);

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
            else if (isWallSliding || (wallDirection != 0 && !isGrounded))
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
            LevelManager.Instance.RestartScene();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
