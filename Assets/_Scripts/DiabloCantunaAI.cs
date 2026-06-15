using UnityEngine;

public class DiabloCantunaAI : MonoBehaviour
{
    [Header("Diablo Settings")]
    public float destructionDelay = 2f;
    
    // Este script actúa como un 'Game Director'. 
    // Los 'Triggers' en el nivel llamarán a estas funciones.

    public void TriggerWallCollapse(GameObject wall)
    {
        Debug.Log("El Diablo destruye el muro: " + wall.name);
        // Ocultar el muro y desactivar su colisión
        wall.SetActive(false);
        // TODO: Instanciar sistema de partículas de escombros (Shuriken)
    }

    public void TriggerFloorRise(GameObject floorPiece)
    {
        Debug.Log("El Diablo levanta el suelo: " + floorPiece.name);
        // Mover el suelo hacia arriba
        floorPiece.transform.position += Vector3.up * 5f;
    }
}
