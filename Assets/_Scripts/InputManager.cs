using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Instancia generada automáticamente por Unity
    private PlayerControls playerControls;

    // Variables públicas para que el PlayerController las lea
    public Vector2 MoveInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintHeld { get; private set; }

    private void Awake()
    {
        playerControls = new PlayerControls();

        // Mapeo de la acción de moverse
        playerControls.Player.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => MoveInput = Vector2.zero;

        // Mapeo de la acción de salto
        playerControls.Player.Jump.performed += ctx => JumpTriggered = true;
        playerControls.Player.Jump.canceled += ctx => JumpTriggered = false;

        // Mapeo de la acción de sprint
        playerControls.Player.Sprint.performed += ctx => SprintHeld = true;
        playerControls.Player.Sprint.canceled += ctx => SprintHeld = false;
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Método para consumir el salto y evitar que se registre múltiples veces por frame
    public void ConsumeJump()
    {
        JumpTriggered = false;
    }
}