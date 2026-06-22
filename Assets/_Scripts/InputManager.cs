using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerControls playerControls;

    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool SlideTriggered { get; private set; }
    public bool InteractTriggered { get; private set; }
    public bool CrouchHeld { get; private set; }
    
    // Controles para Linterna y Lanzamiento
    public bool FlashlightTriggered { get; private set; }
    public bool AimHeld { get; private set; }
    public bool ThrowTriggered { get; private set; }

    private void Awake()
    {
        playerControls = new PlayerControls();

        // Movimiento
        playerControls.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        // Salto
        playerControls.Player.Jump.started += ctx => { JumpTriggered = true; JumpHeld = true; };
        playerControls.Player.Jump.canceled += ctx => JumpHeld = false;

        // Sprint
        playerControls.Player.Sprint.started += ctx => SprintHeld = true;
        playerControls.Player.Sprint.canceled += ctx => SprintHeld = false;
        
        // Acciones Adicionales
        playerControls.Player.Slide.started += ctx => SlideTriggered = true;
        playerControls.Player.Interact.started += ctx => InteractTriggered = true;

        // Agacharse (Comentado temporalmente para arreglar el error de compilación)
        playerControls.Player.Crouch.started += ctx => CrouchHeld = true;
        playerControls.Player.Crouch.canceled += ctx => CrouchHeld = false;

        // Linterna y Lanzamiento
        playerControls.Player.Flashlight.started += ctx => FlashlightTriggered = true;
        playerControls.Player.Aim.started += ctx => AimHeld = true;
        playerControls.Player.Aim.canceled += ctx => { AimHeld = false; ThrowTriggered = true; };
    }

    private void OnEnable() => playerControls.Enable();
    private void OnDisable() => playerControls.Disable();

    public void ConsumeJump() => JumpTriggered = false;
    public void ConsumeSlide() => SlideTriggered = false;
    public void ConsumeInteract() => InteractTriggered = false;
    public void ConsumeFlashlight() => FlashlightTriggered = false;
    public void ConsumeThrow() => ThrowTriggered = false;
}