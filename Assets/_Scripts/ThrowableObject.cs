using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour
{
    public float noiseIntensity = 10f;
    private bool hasHit = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (!hasHit)
        {
            hasHit = true;
            
            // Si golpea directamente al enemigo, lo aturde (según el GDD)
            BaseEnemyAI enemy = collision.gameObject.GetComponentInParent<BaseEnemyAI>();
            if (enemy != null)
            {
                enemy.ApplyStun();
            }

            // Avisar a la IA que hubo un ruido
            AIPerceptionManager.Instance?.ReportNoise(transform.position, noiseIntensity);
            
            // TODO: Reproducir partícula / sonido de rotura (Shuriken / AudioSource)
            
            // Destruir el objeto después de un tiempo para no saturar memoria
            Destroy(gameObject, 2f);
        }
    }
}
