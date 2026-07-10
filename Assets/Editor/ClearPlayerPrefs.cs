using UnityEngine;
using UnityEditor;

public class ClearPlayerPrefs
{
    [MenuItem("Tools/Borrar Partida Guardada (Reset Checkpoints)")]
    public static void ClearPrefs()
    {
        PlayerPrefs.DeleteKey("LastCheckpoint");
        PlayerPrefs.Save();
        Debug.Log("¡Punto de control borrado! El juego empezará desde el inicio la próxima vez.");
    }
}
