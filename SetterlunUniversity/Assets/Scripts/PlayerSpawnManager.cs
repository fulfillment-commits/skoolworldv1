using UnityEngine;
using UnityEngine.SceneManagement;
using Invector.vCharacterController;
using System.Collections;
using System.Linq;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
            
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[PlayerSpawnManager] Scene Loaded: {scene.name}. Checking for pending spawn...");
        
        // Try to get pending spawn point when a new scene loads
        if (SceneTransitionManager.Instance != null)
        {
            string target = SceneTransitionManager.Instance.GetTargetSpawnPoint();
            if (!string.IsNullOrEmpty(target))
            {
                Debug.Log($"[PlayerSpawnManager] Found target spawn from SceneTransitionManager: {target}");
                pendingSpawnPoint = target;
                StopAllCoroutines();
                StartCoroutine(SpawnRoutine(pendingSpawnPoint));
                StartCoroutine(InitializationRoutine());
            }
            else
            {
                Debug.Log("[PlayerSpawnManager] No pending spawn point found in SceneTransitionManager.");
            }
        }
    }

    private string pendingSpawnPoint = "";

    private void Start()
    {
        Debug.Log($"[PlayerSpawnManager] Start in scene: {SceneManager.GetActiveScene().name}");
        
        // Initial check for the first scene
        if (SceneTransitionManager.Instance != null)
        {
            string target = SceneTransitionManager.Instance.GetTargetSpawnPoint();
            if (!string.IsNullOrEmpty(target))
            {
                pendingSpawnPoint = target;
            }
        }

        if (!string.IsNullOrEmpty(pendingSpawnPoint))
        {
            Debug.Log($"[PlayerSpawnManager] Starting SpawnRoutine for {pendingSpawnPoint} from Start()");
            StartCoroutine(SpawnRoutine(pendingSpawnPoint));
            StartCoroutine(InitializationRoutine());
        }
    }

    private IEnumerator InitializationRoutine()
    {
        // Wait a few frames for everything to settle
        yield return new WaitForSeconds(0.5f); // Increased delay
        if (!string.IsNullOrEmpty(pendingSpawnPoint))
        {
            Debug.Log($"[PlayerSpawnManager] Running InitializationRoutine (safety net) for {pendingSpawnPoint}");
            StartCoroutine(SpawnRoutine(pendingSpawnPoint));
        }
    }

    public void CheckForSpawnPoint(string targetPoint)
    {
        if (!string.IsNullOrEmpty(targetPoint))
        {
            Debug.Log($"[PlayerSpawnManager] CheckForSpawnPoint called manually for: {targetPoint}");
            pendingSpawnPoint = targetPoint;
            StartCoroutine(SpawnRoutine(targetPoint));
        }
    }

    private IEnumerator SpawnRoutine(string pointName)
    {
        Debug.Log($"[PlayerSpawnManager] === SPAWN ROUTINE START: {pointName} ===");

        // Wait for player to be instantiated/found
        GameObject player = null;
        int attempts = 0;
        while (player == null && attempts < 60)
        {
            var rfManager = FindObjectOfType<ReferencesManager>();
            if (rfManager != null)
            {
                player = rfManager.player;
            }
            else
            {

                // 1. Try finding Invector Controller FIRST (most reliable for real player)
                var controller = Resources.FindObjectsOfTypeAll<vThirdPersonController>()
                    .FirstOrDefault(c => !c.name.Contains("Preview") && !c.name.Contains("UI"));

                if (controller != null)
                {
                    player = controller.gameObject;
                }
            }

            // 2. Try Tag SECOND
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            
            // 3. Try Name THIRD
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player == null)
            {
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (player == null)
        {
            Debug.LogError($"[PlayerSpawnManager] Player NOT FOUND in scene {SceneManager.GetActiveScene().name} after 60 attempts!");
            yield break;
        }

        Debug.Log($"[PlayerSpawnManager] Found Player object: {player.name}. Attempting to find Spawn Point: {pointName}");

        // Find the spawn point object
        GameObject point = null;
        
        Debug.Log($"Player Found {player.name} Spawn Point: {pointName}");
        // Use a very aggressive search for the point
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        point = allObjects.FirstOrDefault(go => go.name == pointName && (go.scene == SceneManager.GetActiveScene() || !go.scene.IsValid()));

        if (point != null)
        {
            Debug.Log($"[PlayerSpawnManager] Found Point: {point.name} at {point.transform.position}. Starting teleport sequence...");
            
            // Disable physics and controllers
            var vController = player.GetComponent<vThirdPersonController>();
            var vMotor = player.GetComponent<vThirdPersonMotor>();
            var rb = player.GetComponent<Rigidbody>();
            var capsule = player.GetComponent<CapsuleCollider>();
            
            if (vController != null) vController.enabled = false;
            if (vMotor != null) vMotor.enabled = false;
            if (rb != null) 
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            if (capsule != null) capsule.enabled = false;

            // Move and Rotate
            player.transform.position = point.transform.position;
            player.transform.rotation = point.transform.rotation;

            // Wait for internal engine updates
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(0.1f);

            // Re-apply position to be safe
            player.transform.position = point.transform.position;
            player.transform.rotation = point.transform.rotation;

            // Re-enable components
            if (rb != null) rb.isKinematic = false;
            if (capsule != null) capsule.enabled = true;
            if (vController != null) vController.enabled = true;
            if (vMotor != null) vMotor.enabled = true;
            
            Debug.Log($"[PlayerSpawnManager] Teleport to {pointName} successful! Final Position: {player.transform.position}");
            
            if (pendingSpawnPoint == pointName) pendingSpawnPoint = "";
        }
        else
        {
            Debug.LogError($"[PlayerSpawnManager] ERROR: Spawn point '{pointName}' not found in hierarchy. Please check for typos or if the object is missing from the Lobby scene.");
        }
    }

    private string GetAllTransformsSummary()
    {
        Transform[] all = GameObject.FindObjectsOfType<Transform>();
        return string.Join(", ", all.Take(20).Select(t => t.name)) + (all.Length > 20 ? "..." : "");
    }

    private string GetSceneObjectsSummary()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        return string.Join(", ", rootObjects.Take(10).Select(o => o.name)) + (rootObjects.Length > 10 ? "..." : "");
    }
}
