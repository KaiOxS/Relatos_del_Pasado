using UnityEngine;
using UnityEngine.AI;

public class MariangulaAI : BaseEnemyAI
{
    [Header("Mariangula Settings")]
    public float petrifiedSpeed = 0f;
    public float stalkingSpeed = 6f;
    
    [Tooltip("Distancia a la que Mariangula intentará acercarse")]
    public float attackRange = 1.5f;

    private Camera mainCamera;
    private LightController playerLight;
    private bool isBeingWatched;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
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
        }
        else
        {
            // Persiguiendo agresivamente
            agent.isStopped = false;
            agent.speed = stalkingSpeed;
            
            if (playerTransform != null)
            {
                agent.SetDestination(playerTransform.position);
            }
        }
    }

    private void HandleFlashlightToggle(bool isOn)
    {
        // Reevaluar inmediatamente si la linterna cambia
        CheckIfWatched();
    }
}
