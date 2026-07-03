using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class StoneCollectible : MonoBehaviour
{
    public int amount = 1;
    [Tooltip("Distancia máxima desde la que se puede recoger (en unidades)")]
    public float pickupRadius = 1.5f;

    private Transform playerTransform;
    private ThrowSystem throwSystem;
    private bool playerInRange = false;

    private void Start()
    {
        // Buscar al jugador en la escena
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
            throwSystem = pc.GetComponent<ThrowSystem>();
        }

        // Asegurarse de que el Collider sea Trigger para no bloquear físicamente
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Verificar distancia manualmente (más fiable que OnTrigger en 2.5D)
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        playerInRange = dist <= pickupRadius;

        if (playerInRange && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Pickup();
        }
    }

    private void Pickup()
    {
        if (throwSystem != null)
        {
            throwSystem.AddStones(amount);
            // TODO: Reproducir sonido de recolección
            Destroy(gameObject);
        }
    }

    // Mostrar el radio de recolección en el editor para depuración
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}

