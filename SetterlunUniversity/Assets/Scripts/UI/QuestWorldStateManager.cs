using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class QuestWorldObjectConfig
{
    public int questNumber;
    public string questDescription; // Just for organization in inspector
    public GameObject[] objectsToActivate;
    public GameObject[] objectsToDeactivate;
}

public class QuestWorldStateManager : MonoBehaviour
{
    public static QuestWorldStateManager Instance { get; private set; }

    [Header("Quest Object Configurations")]
    [SerializeField] private List<QuestWorldObjectConfig> questConfigs = new List<QuestWorldObjectConfig>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // This is usually placed in the WorldScene, so no DontDestroyOnLoad if you only want it in one scene.
        // But if you want it persistent across all scenes where quests affect objects:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RefreshWorldState();
    }

    /// <summary>
    /// Refreshes the world objects based on the current completion status of all quests.
    /// Should be called when entering the world or after a quest is completed.
    /// </summary>
    public void RefreshWorldState()
    {
        if (OnboardingQuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestWorldStateManager] OnboardingQuestManager.Instance is null. Skipping refresh.");
            return;
        }

        Debug.Log("[QuestWorldStateManager] Refreshing world state based on quest progress...");

        // We iterate through all quests in order to ensure the correct state is applied.
        // Usually, later quests might override visibility of objects from earlier quests.
        foreach (var config in questConfigs.OrderBy(c => c.questNumber))
        {
            bool isCompleted = OnboardingQuestManager.Instance.IsQuestCompleted(config.questNumber);
            ApplyConfigState(config, isCompleted);
        }
    }

    /// <summary>
    /// Applies the state for a specific quest number.
    /// </summary>
    public void RefreshQuestState(int questNumber)
    {
        var config = questConfigs.Find(c => c.questNumber == questNumber);
        if (config != null && OnboardingQuestManager.Instance != null)
        {
            bool isCompleted = OnboardingQuestManager.Instance.IsQuestCompleted(questNumber);
            ApplyConfigState(config, isCompleted);
        }
    }

    private void ApplyConfigState(QuestWorldObjectConfig config, bool isCompleted)
    {
        // If quest is completed, activate the 'Activate' list and deactivate the 'Deactivate' list.
        // If quest is NOT completed, we usually do the opposite (hide what should be shown).
        
        if (config.objectsToActivate != null)
        {
            foreach (var obj in config.objectsToActivate)
            {
                if (obj != null) obj.SetActive(isCompleted);
            }
        }

        if (config.objectsToDeactivate != null)
        {
            foreach (var obj in config.objectsToDeactivate)
            {
                if (obj != null) obj.SetActive(!isCompleted);
            }
        }
        
        Debug.Log($"[QuestWorldStateManager] Applied state for Quest {config.questNumber}. Completed: {isCompleted}");
    }
}
