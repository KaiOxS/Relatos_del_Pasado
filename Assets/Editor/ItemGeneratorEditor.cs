using UnityEngine;
using UnityEditor;
using System.IO;

public class ItemGeneratorEditor : EditorWindow
{
    [MenuItem("Relatos/Generar Items del GDD")]
    public static void GenerateItems()
    {
        string folderPath = "Assets/Prefabs/Items";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Items");
        }

        string[] itemNames = System.Enum.GetNames(typeof(InventorySystem.ItemType));

        foreach (string itemName in itemNames)
        {
            string prefabPath = folderPath + "/" + itemName + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = itemName;

                Collider col = obj.GetComponent<Collider>();
                col.isTrigger = true; // El jugador atraviesa para recoger

                CollectibleItem collectible = obj.AddComponent<CollectibleItem>();
                collectible.itemType = (InventorySystem.ItemType)System.Enum.Parse(typeof(InventorySystem.ItemType), itemName);
                
                // Determinar si es un artefacto clave
                if (itemName == "Tripas" || itemName == "Velo" || itemName == "Sombrero" || itemName == "PiedraFaltante")
                {
                    collectible.isArtifact = true;
                    // Cambiar color para que destaque
                    obj.GetComponent<MeshRenderer>().sharedMaterial.color = Color.yellow;
                }

                // Guardar como prefab
                PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
                DestroyImmediate(obj);
            }
        }
        
        Debug.Log("¡Prefabs de ítems generados con éxito en " + folderPath + "!");
    }
}
