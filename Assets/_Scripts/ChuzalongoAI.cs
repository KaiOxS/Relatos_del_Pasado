using UnityEngine;
using UnityEngine.AI;

public class ChuzalongoAI : BaseEnemyAI
{
    [Header("Chuzalongo Settings")]
    public float timeAllowedOnGround = 3f;
    public float dropSpeed = 15f;
    public LayerMask groundLayer;
    
    private float playerGroundTimer = 0f;
    private PlayerController playerScript;
    private bool isExecutingDropKill = false;

    protected override void Start()
    {
        base.Start();
        if (playerTransform != null)
        {
            playerScript = playerTransform.GetComponent<PlayerController>();
        }
    }

    protected override void UpdatePatrol()
    {
        // Se mueve por la parte superior imitando la posición X del jugador
        if (playerTransform != null && !isExecutingDropKill)
        {
            Vector3 targetPos = new Vector3(playerTransform.position.x, transform.position.y, transform.position.z);
            agent.SetDestination(targetPos);
            agent.speed = patrolSpeed;
        }
    }

    protected override void UpdateChase()
    {
        // Su persecución es el Drop Kill
        if (isExecutingDropKill)
        {
            agent.enabled = false; // Desactiva NavMesh para caer libremente
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, dropSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, playerTransform.position) < 1f)
            {
                ExecuteInstaKill();
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        if (currentState != AIState.Stunned && !isExecutingDropKill)
        {
            CheckPlayerOnGround();
        }
    }

    private void CheckPlayerOnGround()
    {
        if (playerScript == null) return;

        // Comprobar si el jugador está en el suelo disparando un rayo desde él
        if (Physics.Raycast(playerTransform.position, Vector3.down, 1.5f, groundLayer))
        {
            playerGroundTimer += Time.deltaTime;
            if (playerGroundTimer >= timeAllowedOnGround)
            {
                InitiateDropKill();
            }
        }
        else
        {
            playerGroundTimer = 0f; // Se resetea si el jugador salta o hace WallJump
        }
    }

    private void InitiateDropKill()
    {
        isExecutingDropKill = true;
        currentState = AIState.Chase;
        Debug.Log("Chuzalongo detectó demasiado tiempo en el suelo. Atacando!");
    }
}
