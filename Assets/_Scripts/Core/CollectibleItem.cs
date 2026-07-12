using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    public InventorySystem.ItemType itemType;
    public int amount = 1;
    public AudioClip pickupSound;
    public bool isArtifact = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItem(itemType.ToString(), amount);
                
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("No hay InventorySystem en la escena para recoger el objeto.");
            }
        }
    }
}
