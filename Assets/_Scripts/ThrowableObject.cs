using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour
{
    public float noiseIntensity = 10f;
    private bool hasHit = false;

    private void OnCollisionEnter(Collision collision)
    {
        HitTarget(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HitTarget(other.gameObject);
    }

    private void HitTarget(GameObject target)
    {
        // Ignorar colisiones con el propio jugador para que no se desactive al instante de ser lanzada
        if (target.CompareTag("Player") || target.transform.root.CompareTag("Player"))
        {
            return;
        }

        // También ignorar otros proyectiles si llegaran a chocar en el aire
        if (target.GetComponent<ThrowableObject>() != null) return;

        if (!hasHit)
        {
            hasHit = true;
            
            Debug.Log("La piedra golpeó a: " + target.name);

            // Si golpea directamente al enemigo, lo aturde (según el GDD)
            BaseEnemyAI enemy = target.GetComponentInParent<BaseEnemyAI>();
            if (enemy != null)
            {
                enemy.stunDuration = 2f; // Forzar a que sean exactamente 2 segundos según la nueva regla
                enemy.ApplyStun();
                Debug.Log("¡Enemigo aturdido por la piedra!");
            }

            // Avisar a la IA que hubo un ruido
            AIPerceptionManager.Instance?.ReportNoise(transform.position, noiseIntensity);
            
            // TODO: Reproducir partícula / sonido de rotura (Shuriken / AudioSource)
            
            // Destruir el objeto después de un tiempo para no saturar memoria
            Destroy(gameObject, 2f);
        }
    }
}
