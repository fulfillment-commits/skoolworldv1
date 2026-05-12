using UnityEngine;
using System.Collections.Generic;
using System;

public class OnboardingQuestManager : MonoBehaviour
{
    public static OnboardingQuestManager Instance { get; private set; }

    private Dictionary<int, bool> completedQuests = new Dictionary<int, bool>();
    private Dictionary<int, string> questDataJson = new Dictionary<int, string>();
    private string currentUserId;
    private string currentUsername;
    private bool isInitialSyncComplete = false;

    [Header("Quest Panel Reference")]
    [SerializeField] private QuestPanelUI questPanelUI;

    private const string PLAYERPREFS_QUEST_PREFIX = "Quest_";
    private const string PLAYERPREFS_QUEST_DATA_PREFIX = "QuestData_";
    private const string PLAYERPREFS_USER_ID = "OnboardingUserId_Str";
    private const string PLAYERPREFS_USERNAME = "OnboardingUsername";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSessionLocal();
    }

    private void LoadSessionLocal()
    {
        currentUserId = PlayerPrefs.GetString(PLAYERPREFS_USER_ID, "");
        currentUsername = PlayerPrefs.GetString(PLAYERPREFS_USERNAME, "");

        if (!string.IsNullOrEmpty(currentUserId))
        {
            LoadQuestProgressFromBackend(currentUserId);
        }
    }

    public void Initialize(string userId, string username = "")
    {
        currentUserId = userId;
        currentUsername = username;

        // Fallback to PlayerPrefs if username is not provided
        if (string.IsNullOrEmpty(currentUsername))
        {
            currentUsername = PlayerPrefs.GetString(PLAYERPREFS_USERNAME, "");
        }
        else
        {
            // If username is provided, save it to PlayerPrefs for persistence
            PlayerPrefs.SetString(PLAYERPREFS_USERNAME, currentUsername);
            PlayerPrefs.Save();
        }

        completedQuests.Clear();
        questDataJson.Clear();

        LoadQuestProgressFromBackend(userId);

        RefreshQuestPanel();
        Debug.Log($"OnboardingQuestManager initialized for User ID: {userId}");
    }

    private void LoadQuestProgressFromBackend(string userId)
    {
        Debug.Log($"[OnboardingQuestManager] Loading quest progress for user: {userId}");
        
        BackendSettings.Instance.Service.GetQuestProgress((progress) => {
            if (progress != null)
            {
                foreach (var step in progress)
                {
                    completedQuests[step.questNumber] = step.completed;
                    questDataJson[step.questNumber] = step.dataJson;
                }
                
                isInitialSyncComplete = true;
                RefreshQuestPanel();
                
                // Force refresh triggers if world is already loaded
                if (QuestWorldStateManager.Instance != null)
                    QuestWorldStateManager.Instance.RefreshWorldState();
            }
        }, (error) => {
            Debug.LogError($"[OnboardingQuestManager] Failed to load progress: {error}");
            isInitialSyncComplete = true; // Still allow game to proceed
        });
    }

    public void CompleteQuest(int questNumber, string dataJson = "")
    {
        if (completedQuests.ContainsKey(questNumber) && completedQuests[questNumber])
        {
            Debug.Log($"Quest {questNumber} already completed.");
            return;
        }

        completedQuests[questNumber] = true;
        questDataJson[questNumber] = dataJson;

        // Save progress via Backend Service
        BackendSettings.Instance.Service.CompleteQuest(questNumber, dataJson, (success, msg) => {
            if (success) Debug.Log($"✅ Quest {questNumber} synced with Backend: {msg}");
            else Debug.LogError($"❌ Failed to sync Quest {questNumber} with Backend: {msg}");
        });

        RefreshQuestPanel();
        
        // Notify World State Manager to update object visibility
        if (QuestWorldStateManager.Instance != null)
        {
            QuestWorldStateManager.Instance.RefreshQuestState(questNumber);
        }

        // Notify Generic Object Manager to update visibility
        var objectManagers = FindObjectsOfType<QuestObjectManager>();
        foreach (var mgr in objectManagers)
        {
            mgr.RefreshObjects();
        }

        Debug.Log($"Quest {questNumber} completed locally!");
        
        // Check if all quests are completed
        if (GetCompletedQuestCount() >= 10)
        {
            OnboardingManager.Instance?.CompleteFullOnboarding();
        }
    }

    private void RouteQuestData(int questNumber, string dataJson)
    {
        if (OnboardingManager.Instance == null) return;

        try
        {
            switch (questNumber)
            {
                case 1: // Brick Claim
                    // Logic handled by the quest screen itself calling OnboardingManager
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error routing quest data: {e.Message}");
        }
    }

    public bool IsQuestCompleted(int questNumber)
    {
        return completedQuests.ContainsKey(questNumber) && completedQuests[questNumber];
    }

    public string GetQuestData(int questNumber)
    {
        if (questDataJson.TryGetValue(questNumber, out string data))
        {
            return data;
        }
        return null;
    }

    public float GetOverallProgress()
    {
        int total = 10;
        int completedCount = 0;
        for (int i = 1; i <= total; i++)
        {
            if (IsQuestCompleted(i)) completedCount++;
        }
        return (float)completedCount / total;
    }

    public int GetCompletedQuestCount()
    {
        int count = 0;
        for (int i = 1; i <= 10; i++)
        {
            if (IsQuestCompleted(i)) count++;
        }
        return count;
    }

    public int GetNextIncompleteQuest()
    {
        for (int i = 1; i <= 10; i++)
        {
            if (!IsQuestCompleted(i)) return i;
        }
        return -1; // All completed
    }

    public void RefreshQuestPanel()
    {
        if (questPanelUI != null)
            questPanelUI.Refresh();
    }

    public void RegisterQuestPanel(QuestPanelUI panel)
    {
        questPanelUI = panel;
        RefreshQuestPanel();
        
        // Force refresh world objects and triggers
        if (QuestWorldStateManager.Instance != null)
        {
            QuestWorldStateManager.Instance.RefreshWorldState();
        }
    }

    public string CurrentUserId => currentUserId;
    public string CurrentUsername => currentUsername;
    public bool IsInitialSyncComplete => isInitialSyncComplete;

    public void SetUserId(string userId)
    {
        currentUserId = userId;
        PlayerPrefs.SetString(PLAYERPREFS_USER_ID, userId);
        PlayerPrefs.Save();
    }

    public void SetUsername(string username)
    {
        currentUsername = username;
        PlayerPrefs.SetString(PLAYERPREFS_USERNAME, username);
        PlayerPrefs.Save();
    }

    public void SetAvatarIndex(int avatarIndex)
    {
        PlayerPrefs.SetInt("OnboardingAvatarIndex", avatarIndex);
        PlayerPrefs.Save();
        Debug.Log($"Avatar index set to: {avatarIndex}");
    }

    [System.Serializable]
    private class OnboardingStepsResponse
    {
        public OnboardingStepItem[] steps;
    }

    [System.Serializable]
    private class OnboardingStepItem
    {
        public int id;
        public string user_id;
        public int step_number;
        public bool completed;
        public string data_json;
    }

    [System.Serializable]
    private class QuestProgressData
    {
        public string user_id;
        public int step_number;
        public bool completed;
        public string data_json;
    }
}