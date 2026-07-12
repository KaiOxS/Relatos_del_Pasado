using UnityEngine;
using UnityEngine.AI;

public class DamaTapadaAI : BaseEnemyAI
{
    [Header("Dama Tapada Settings")]
    public float auraRadius = 0.6f;
    public float auraLethalTime = 3f;
    
    [Header("Flying Settings")]
    public float detectionRadius = 9f;
    public float reactionTime = 1f;
    public float flightSpeed = 1.5f;
    public float chaseFlightSpeed = 3.5f;
    public float floatAmplitude = 0.5f;
    public float floatFrequency = 2f;
    
    private float initialY;
    private float currentReactionTimer = 0f;
    private float timeInAura = 0f;
    private bool playerInAura = false;
    
    private PlayerController playerCtrl;
    private Vector3 randomWanderTarget;
    private bool hasWanderTarget = false;
    private Transform veloTransform;
    
    // Puntos de patrullaje simple
    public Transform[] patrolPoints;
    private int currentPatrolIndex;

    private void OnValidate()
    {
        // Fuerza a que el Inspector actualice el valor si se quedó guardado uno muy grande
        if (auraRadius > 2f) auraRadius = 0.6f;
    }

    protected override void Start()
    {
        base.Start();
        if (agent != null) agent.enabled = false; // Desactivar NavMeshAgent para volar libremente
        initialY = transform.position.y;
        playerCtrl = FindObjectOfType<PlayerController>();

        // Buscar el velo en la escena
        CollectibleItem[] items = FindObjectsOfType<CollectibleItem>();
        foreach (var item in items)
        {
            if (item.itemType == InventorySystem.ItemType.Velo)
            {
                veloTransform = item.transform;
                break;
            }
        }
    }

    protected override void UpdatePatrol()
    {
        if (playerTransform == null) return;
        
        float currentDetectionRadius = detectionRadius;
        float currentReactionTime = reactionTime;

        // Escalar agresividad si el jugador está cerca del velo
        if (veloTransform != null)
        {
            float distanceToVelo = Vector3.Distance(playerTransform.position, veloTransform.position);
            if (distanceToVelo < 20f)
            {
                // Modo más agresivo: aumenta el rango de visión y reacciona al instante
                currentDetectionRadius *= 1.8f;
                currentReactionTime *= 0.2f; 
            }
            else
            {
                // Modo menos agresivo (relajada)
                currentDetectionRadius *= 0.7f;
                currentReactionTime *= 1.5f;
            }
        }
        
        // Modificar radio según estado del jugador
        if (playerCtrl != null)
        {
            if (playerCtrl.currentState == PlayerController.PlayerState.Crouch || 
                playerCtrl.currentState == PlayerController.PlayerState.CrouchWalk)
            {
                currentDetectionRadius *= 0.35f; // 65% más difícil de detectar si está agachado
            }
            else if (playerCtrl.currentState == PlayerController.PlayerState.Walk ||
                     playerCtrl.currentState == PlayerController.PlayerState.Idle)
            {
                currentDetectionRadius *= 0.7f; // 30% más difícil si camina o está quieto sin agacharse
            }
            // Si corre, rueda o salta, usa el radio de detección completo (100%)
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= currentDetectionRadius)
        {
            currentReactionTimer += Time.deltaTime;
            if (currentReactionTimer >= currentReactionTime)
            {
                currentState = AIState.Chase;
                currentReactionTimer = 0f;
                return;
            }
        }
        else
        {
            currentReactionTimer = Mathf.Max(0, currentReactionTimer - Time.deltaTime);
        }

        // Vuelo de patrulla hacia los puntos
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Transform target = patrolPoints[currentPatrolIndex];
            Vector3 targetPos = target.position;
            
            // Simular vuelo suave
            float newY = initialY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            targetPos.y = newY;
            targetPos.z = transform.position.z; // Mantener Z
            
            transform.position = Vector3.MoveTowards(transform.position, targetPos, flightSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPos) < 1f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
        else
        {
            // Volar por la escena aleatoriamente en el eje X si no hay puntos (cubriendo todo el nivel)
            if (!hasWanderTarget)
            {
                float randomX = transform.position.x + Random.Range(-40f, 40f);
                randomWanderTarget = new Vector3(randomX, initialY, transform.position.z);
                hasWanderTarget = true;
            }

            Vector3 targetPos = randomWanderTarget;
            targetPos.y = initialY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            
            transform.position = Vector3.MoveTowards(transform.position, targetPos, flightSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPos) < 1f)
            {
                hasWanderTarget = false;
            }
        }
    }

    protected override void UpdateChase()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > detectionRadius * 1.5f) // Margen para no perderlo de inmediato
        {
            currentState = AIState.Patrol;
            return;
        }

        Vector3 targetPos = playerTransform.position;
        // Volar hacia el jugador flotando
        targetPos.y += Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        targetPos.z = transform.position.z; // Mantener Z
        
        transform.position = Vector3.MoveTowards(transform.position, targetPos, chaseFlightSpeed * Time.deltaTime);
    }

    public override void ApplyStun()
    {
        currentState = AIState.Stunned;
        stunTimer = stunDuration;
    }

    protected override void UpdateStunned()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            currentState = AIState.Chase;
        }
        
        Vector3 targetPos = transform.position;
        targetPos.y += Mathf.Sin(Time.time * floatFrequency * 0.5f) * (floatAmplitude * 0.2f);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime);
    }

    protected override void HandleNoise(Vector3 noisePosition, float intensity)
    {
        if (currentState != AIState.Stunned)
        {
            currentState = AIState.Chase;
        }
    }

    protected override void Update()
    {
        base.Update();
        CheckAura();
    }

    private void CheckAura()
    {
        if (playerTransform == null || currentState == AIState.Stunned) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= auraRadius)
        {
            if (!playerInAura)
            {
                playerInAura = true;
                InputManager im = FindObjectOfType<InputManager>();
                if (im != null) im.ControlsInverted = true;
                Debug.Log("Jugador entró en el aura de la Dama Tapada. Controles invertidos.");
            }

            timeInAura += Time.deltaTime;
            
            if (timeInAura >= auraLethalTime)
            {
                ExecuteInstaKill();
            }
        }
        else
        {
            if (playerInAura)
            {
                playerInAura = false;
                timeInAura = 0f;
                InputManager im = FindObjectOfType<InputManager>();
                if (im != null) im.ControlsInverted = false;
                Debug.Log("Jugador salió del aura.");
            }
        }
    }

    protected override void ExecuteInstaKill()
    {
        Debug.Log("Insta-Kill ejecutado por Dama Tapada. Regresando al principio del nivel.");
        PlayerPrefs.DeleteKey("LastCheckpoint");
        PlayerPrefs.DeleteKey("PlayerHealth"); // Restaurar vida
        PlayerPrefs.DeleteKey("StoneCount"); // Restaurar inventario de piedras
        SaveSystem.ClearInventory(); // Reiniciar consumibles
        PlayerPrefs.Save();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneAsync("Greybox_Prototipo");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Greybox_Prototipo");
        }
    }
    
    // Visualización en el Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, auraRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
