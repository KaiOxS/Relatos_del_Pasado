using UnityEngine;

public static class SaveSystem
{
    private const string LAST_SCENE_KEY = "LastSavedScene";

    public static void SaveCurrentScene(string sceneName)
    {
        PlayerPrefs.SetString(LAST_SCENE_KEY, sceneName);
        PlayerPrefs.Save();
        Debug.Log("Partida guardada en la escena: " + sceneName);
    }

    public static string GetLastSavedScene()
    {
        if (PlayerPrefs.HasKey(LAST_SCENE_KEY))
        {
            return PlayerPrefs.GetString(LAST_SCENE_KEY);
        }
        return string.Empty;
    }

    public static void ClearProgress()
    {
        PlayerPrefs.DeleteKey(LAST_SCENE_KEY);
        PlayerPrefs.DeleteKey("LastCheckpoint");
        PlayerPrefs.DeleteKey("PlayerHealth");
        PlayerPrefs.DeleteKey("StoneCount");
        
        ClearInventory();

        PlayerPrefs.Save();
        Debug.Log("Progreso borrado.");
    }

    public static void ClearInventory()
    {
        foreach (string name in System.Enum.GetNames(typeof(InventorySystem.ItemType)))
        {
            PlayerPrefs.DeleteKey("Inv_" + name);
        }
        PlayerPrefs.Save();
    }
}
