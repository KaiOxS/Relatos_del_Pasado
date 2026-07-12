using UnityEngine;
using UnityEngine.AI;

public class DamaTapadaAI : BaseEnemyAI
{
    [Header("Dama Tapada Settings")]
    public float auraRadius = 5f;
    public float auraLethalTime = 3f;
    
    private float timeInAura = 0f;
    private bool playerInAura = false;
    
    // Puntos de patrullaje simple
    public Transform[] patrolPoints;
    private int currentPatrolIndex;

    protected override void UpdatePatrol()
    {
        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (patrolPoints.Length == 0) return;

        if (agent.remainingDistance < 0.5f && !agent.pathPending)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    protected override void UpdateChase()
    {
        // La Dama Tapada no persigue activamente, emite un aura y patrulla
        UpdatePatrol();
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
    }
}
