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
            
            // Avisar a la IA que hubo un ruido
            AIPerceptionManager.Instance?.ReportNoise(transform.position, noiseIntensity);
            
            // TODO: Reproducir partícula / sonido de rotura (Shuriken / AudioSource)
            
            // Destruir el objeto después de un tiempo para no saturar memoria
            Destroy(gameObject, 2f);
        }
    }
}
