using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Name used by SceneTransitionManager to identify this spawn point")]
    public string pointName;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawRay(transform.position, transform.forward * 1f);
    }
}
