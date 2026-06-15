using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class BaseEnemyAI : MonoBehaviour
{
    protected NavMeshAgent agent;
    protected Transform playerTransform;

    public enum AIState { Patrol, Chase, Stunned }
    public AIState currentState;

    [Header("Base Settings")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;
    public float stunDuration = 3f;
    protected float stunTimer;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // En 2.5D a menudo no queremos que el agente rote libremente en todos los ejes
        agent.updateUpAxis = false;
        agent.updateRotation = false; 
    }

    protected virtual void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null) playerTransform = player.transform;

        // Suscribirse al gestor de ruido
        if (AIPerceptionManager.Instance != null)
        {
            AIPerceptionManager.Instance.OnNoiseHeard += HandleNoise;
        }
    }

    protected virtual void OnDestroy()
    {
        if (AIPerceptionManager.Instance != null)
        {
            AIPerceptionManager.Instance.OnNoiseHeard -= HandleNoise;
        }
    }

    protected virtual void Update()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Stunned:
                UpdateStunned();
                break;
        }
    }

    // Cada ente implementará su propia forma de patrullar y perseguir (ej. Mariangula vs Chuzalongo)
    protected abstract void UpdatePatrol();
    protected abstract void UpdateChase();
    
    protected virtual void UpdateStunned()
    {
        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0)
        {
            currentState = AIState.Chase;
            agent.isStopped = false;
        }
    }

    public virtual void ApplyStun()
    {
        currentState = AIState.Stunned;
        stunTimer = stunDuration;
        agent.isStopped = true; // Detener movimiento en NavMesh
    }

    protected virtual void HandleNoise(Vector3 noisePosition, float intensity)
    {
        // Reacción genérica: Si no está aturdido, ir a investigar el ruido
        if (currentState != AIState.Stunned)
        {
            agent.SetDestination(noisePosition);
        }
    }

    // El collider de Narrow Phase usará esto al tocar al jugador
    protected void ExecuteInstaKill()
    {
        Debug.Log("Insta-Kill ejecutado por " + gameObject.name);
        // TODO: Llamar al LevelManager para reiniciar el checkpoint
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentState != AIState.Stunned)
        {
            ExecuteInstaKill();
        }
    }
}
