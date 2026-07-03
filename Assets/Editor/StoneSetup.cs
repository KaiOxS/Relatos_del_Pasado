using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class StoneSetup
{
    [MenuItem("Relatos/Autoconfigurar Inventario de Piedras")]
    public static void SetupStones()
    {
        string prefabFolder = "Assets/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        string spritePath = "Assets/Sprites/Objects/Piedra.png";

        // Forzar que Unity lo importe como Sprite si todavía no lo es
        TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("No se encontró el archivo Piedra.png en: " + spritePath);
            return;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport(); // Método más fiable que AssetDatabase.ImportAsset
            AssetDatabase.Refresh();   // Forzar a Unity a refrescar la base de datos
        }

        // Pequeño truco: cargar la textura primero y luego convertirla a Sprite
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
        Sprite stoneSprite = null;

        if (tex != null)
        {
            // Intentar cargar el sub-asset Sprite
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(spritePath);
            foreach (Object asset in allAssets)
            {
                if (asset is Sprite s)
                {
                    stoneSprite = s;
                    break;
                }
            }
        }

        if (stoneSprite == null)
        {
            Debug.LogError("No se pudo cargar Piedra.png como Sprite. Abre el Inspector de ese archivo, cambia Texture Type a 'Sprite (2D and UI)' y presiona Apply. Luego vuelve a ejecutar este menú.");
            return;
        }

        // 1. Crear el Proyectil (Piedra que lanza el jugador)
        GameObject projObj = new GameObject("PiedraProjectile");
        SpriteRenderer projSr = projObj.AddComponent<SpriteRenderer>();
        projSr.sprite = stoneSprite;
        projObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Ajustar tamaño

        SphereCollider projCol = projObj.AddComponent<SphereCollider>();
        projCol.radius = 0.3f; // Radio del collider

        Rigidbody projRb = projObj.AddComponent<Rigidbody>();
        projRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        ThrowableObject throwable = projObj.AddComponent<ThrowableObject>();
        throwable.noiseIntensity = 15f;

        GameObject savedProj = PrefabUtility.SaveAsPrefabAsset(projObj, prefabFolder + "/PiedraProjectile.prefab");
        Object.DestroyImmediate(projObj);

        // 2. Crear el Recolectable (Piedra que se recoge del suelo)
        GameObject collObj = new GameObject("PiedraCollectible");
        SpriteRenderer collSr = collObj.AddComponent<SpriteRenderer>();
        collSr.sprite = stoneSprite;
        collObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

        SphereCollider collCol = collObj.AddComponent<SphereCollider>();
        collCol.radius = 0.5f;
        collCol.isTrigger = true; // Es un trigger para recogerla

        collObj.AddComponent<StoneCollectible>().amount = 1;

        GameObject savedColl = PrefabUtility.SaveAsPrefabAsset(collObj, prefabFolder + "/PiedraCollectible.prefab");
        Object.DestroyImmediate(collObj);

        // 3. Asignar el proyectil al jugador y asegurar que empiece con 0 piedras
        ThrowSystem ts = Object.FindObjectOfType<ThrowSystem>();
        if (ts != null)
        {
            ts.projectilePrefab = savedProj;
            ts.stoneCount = 0; // Empieza sin piedras
            EditorUtility.SetDirty(ts);
        }

        // 4. Crear la UI del contador en la esquina superior izquierda
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            // Remover la UI anterior si existe para evitar duplicados
            Transform oldUI = canvas.transform.Find("StoneUI_Container");
            if (oldUI != null) Object.DestroyImmediate(oldUI.gameObject);

            GameObject container = new GameObject("StoneUI_Container");
            container.transform.SetParent(canvas.transform, false);
            RectTransform rect = container.AddComponent<RectTransform>();
            
            // Anclar en Top-Left
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(20, -20); // Margen de 20px
            rect.sizeDelta = new Vector2(150, 60); // Tamaño del contenedor

            // Icono de la piedra
            GameObject icon = new GameObject("StoneIcon");
            icon.transform.SetParent(container.transform, false);
            Image img = icon.AddComponent<Image>();
            img.sprite = stoneSprite;
            img.preserveAspect = true;
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(60, 0);

            // Texto del contador
            GameObject textObj = new GameObject("StoneText");
            textObj.transform.SetParent(container.transform, false);
            Text txt = textObj.AddComponent<Text>();
            txt.text = "0"; // Valor inicial
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 32;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            
            // Borde negro para que se lea en cualquier fondo
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
            
            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0, 0);
            txtRect.anchorMax = new Vector2(1, 1);
            txtRect.pivot = new Vector2(0, 0.5f);
            txtRect.offsetMin = new Vector2(70, 0); // 70 px de margen a la izquierda para no pisar el icono
            txtRect.offsetMax = Vector2.zero;

            // Añadir o actualizar el script StoneUI
            StoneUI stoneUI = canvas.gameObject.GetComponent<StoneUI>();
            if (stoneUI == null) stoneUI = canvas.gameObject.AddComponent<StoneUI>();
            stoneUI.stoneText = txt;
            
            EditorUtility.SetDirty(canvas);
        }

        // 5. Instanciar una piedra recolectable en el escenario cerca del jugador
        if (ts != null)
        {
            GameObject spawnedStone = (GameObject)PrefabUtility.InstantiatePrefab(savedColl);
            // Colocarla 2 unidades a la derecha y un poco arriba del jugador
            spawnedStone.transform.position = ts.transform.position + new Vector3(2f, 1f, 0f);
            Debug.Log("¡Inventario de Piedras configurado con éxito! Se ha creado una piedra en el escenario para recogerla.");
        }
    }
}
