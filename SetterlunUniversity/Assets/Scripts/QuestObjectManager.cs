using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[System.Serializable]
public class QuestStateConfig
{
    public int questNumber;
    public string description; 
    
    [Header("Objects for this Quest")]
    [Tooltip("These objects will be ACTIVE when this is the current active quest.")]
    public GameObject[] objectsToActivate;
    
    [Tooltip("These objects will be INACTIVE when this is the current active quest.")]
    public GameObject[] objectsToDeactivate;
}

public class QuestObjectManager : MonoBehaviour
{
    [Header("Quest Scene States")]
    [Tooltip("Define exactly what should be visible for each specific quest state.")]
    [SerializeField] private List<QuestStateConfig> questConfigs = new List<QuestStateConfig>();

    private void Start()
    {
        RefreshObjects();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshObjects();
    }

    public void RefreshObjects()
    {
        if (OnboardingQuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestObjectManager] OnboardingQuestManager.Instance is null. Cannot refresh objects.");
            return;
        }

        int currentQuest = OnboardingQuestManager.Instance.GetNextIncompleteQuest();
        Debug.Log($"[QuestObjectManager] Refreshing for Current Quest: {currentQuest} in scene: {gameObject.scene.name}");

        // Find the configuration for the current quest
        QuestStateConfig currentConfig = questConfigs.Find(c => c.questNumber == currentQuest);

        if (currentConfig != null)
        {
            // 1. Activate objects defined for this specific quest
            if (currentConfig.objectsToActivate != null)
            {
                foreach (var obj in currentConfig.objectsToActivate)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }

            // 2. Deactivate objects defined for this specific quest
            if (currentConfig.objectsToDeactivate != null)
            {
                foreach (var obj in currentConfig.objectsToDeactivate)
                {
                    if (obj != null) obj.SetActive(false);
                }
            }
        }
        else
        {
            Debug.Log($"[QuestObjectManager] No specific configuration found for Quest {currentQuest}.");
        }
    }
}
