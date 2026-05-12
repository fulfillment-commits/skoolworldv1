using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerTeleport : MonoBehaviour
{
    private bool isInsideTrigger = false;
    private Transform currentTrigger;

    void Awake()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Clean up listener to prevent memory leaks or errors on scene change
        if (GamePlayUIManager.Instance != null && GamePlayUIManager.Instance.Door != null)
        {
            Button btn = GamePlayUIManager.Instance.Door.GetComponent<Button>();
            if (btn == null) btn = GamePlayUIManager.Instance.Door.GetComponentInChildren<Button>();
            
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnButtonPressed);
            }
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        LinkButton();
    }

    void Start()
    {
        LinkButton();
    }

    private void LinkButton()
    {
        if (GamePlayUIManager.Instance != null && GamePlayUIManager.Instance.Door != null)
        {
            GamePlayUIManager.Instance.Door.SetActive(false);
            
            // Automatically add listener to the button component on the Door object
            Button btn = GamePlayUIManager.Instance.Door.GetComponent<Button>();
            if (btn == null) btn = GamePlayUIManager.Instance.Door.GetComponentInChildren<Button>();
            
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnButtonPressed); // Avoid duplicates
                btn.onClick.AddListener(OnButtonPressed);
                Debug.Log("[PlayerTeleport] Successfully linked Door button listener.");
            }
            else
            {
                Debug.LogWarning("[PlayerTeleport] Door GameObject in GamePlayUIManager is missing a Button component!");
            }
        }
    }

    void Update()
    {
        if (isInsideTrigger && currentTrigger != null && GamePlayUIManager.Instance != null && GamePlayUIManager.Instance.Door != null)
        {
            // Check direction relative to trigger
            float playerZ = transform.forward.z;
            float triggerZ = currentTrigger.forward.z;

            // Show button if facing the same direction as trigger forward
            if (playerZ * triggerZ > 0)
            {
                if (!GamePlayUIManager.Instance.Door.activeSelf)
                    GamePlayUIManager.Instance.Door.SetActive(true);
            }
            else
            {
                if (GamePlayUIManager.Instance.Door.activeSelf)
                    GamePlayUIManager.Instance.Door.SetActive(false);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Teleport"))
        {
            isInsideTrigger = true;
            currentTrigger = other.transform;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Teleport"))
        {
            isInsideTrigger = false;
            if (GamePlayUIManager.Instance != null && GamePlayUIManager.Instance.Door != null)
                GamePlayUIManager.Instance.Door.SetActive(false);
            currentTrigger = null;
        }
    }

    public void OnButtonPressed()
    {
        if (!isInsideTrigger || currentTrigger == null) return;

        string targetScene = "";
        string spawnPoint = null;

        // Try to get target info from the trigger object
        TeleportTarget targetInfo = currentTrigger.GetComponent<TeleportTarget>();
        if (targetInfo != null)
        {
            targetScene = targetInfo.targetScene;
            spawnPoint = targetInfo.spawnPointName;
        }
        else
        {
            // Fallback to object name if no TeleportTarget component found
            targetScene = currentTrigger.name;
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[PlayerTeleport] No target scene specified for trigger: {currentTrigger.name}");
            return;
        }

        // Auto-set spawn point for WorldScene if not explicitly set
        if (targetScene == "WorldScene" && string.IsNullOrEmpty(spawnPoint))
        {
            spawnPoint = "UniversityMainDoor";
        }

        // Smoothly transition to the target scene
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetScene, spawnPoint);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        }
    }
}
