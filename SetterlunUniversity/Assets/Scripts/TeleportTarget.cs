using UnityEngine;

public class TeleportTarget : MonoBehaviour
{
    [Tooltip("The exact name of the scene to load when this trigger is activated")]
    public string targetScene;
    
    [Tooltip("Optional: The name of the spawn point in the target scene")]
    public string spawnPointName;
}
