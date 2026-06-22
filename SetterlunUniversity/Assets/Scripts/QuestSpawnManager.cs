using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuestSpawnManager : MonoBehaviour
{
    public static QuestSpawnManager Instance { get; private set; }

    [Header("Scene Configuration")]
    public string lobbySceneName = "Lobby";
    public string worldSceneName = "WorldScene";

    [Header("Quest Routing")]
    [Tooltip("Quests that should be performed in the Lobby scene")]
    public int[] lobbyQuests = new int[] { 3, 4 }; 

    [Header("Spawn Points")]
    public string[] questSpawnPoints = new string[] {
        "Spawn_Quest1", // Start / Tour
        "Spawn_Quest2", // Brick Claim
        "Spawn_Quest3", // Personal Profile
        "Spawn_Quest4", // Business Profile
        "Spawn_Quest5", // Leads & Sales
        "Spawn_Quest6", // Operations
        "Spawn_Quest7", // Authority
        "Spawn_Quest8", // Struggles
        "Spawn_Quest9", // Goals
        "Spawn_Quest10", // Self Awareness
        "Spawn_Final"    // All completed
    };

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
    }

    public void SpawnForNextQuest()
    {
        if (OnboardingQuestManager.Instance == null)
        {
            Debug.LogError("[QuestSpawnManager] OnboardingQuestManager.Instance is null!");
            return;
        }

        int nextQuest = OnboardingQuestManager.Instance.GetNextIncompleteQuest();
        Debug.Log($"[QuestSpawnManager] Deciding spawn for Quest {nextQuest}");
        
        string spawnPointName = "";
        
        if (nextQuest == -1) // All completed
        {
            spawnPointName = "Spawn_Final";
        }
        else
        {
            // Use the array to get the name, adjusting for 1-based index
            int index = nextQuest - 1;
            if (index >= 0 && index < questSpawnPoints.Length)
            {
                spawnPointName = questSpawnPoints[index];
            }
            else
            {
                spawnPointName = "Spawn_Quest" + nextQuest; // Fallback
            }
        }

        // Determine which scene this quest belongs to
        bool isLobbyQuest = false;
        if (nextQuest != -1)
        {
            foreach (int q in lobbyQuests)
            {
                if (q == nextQuest)
                {
                    isLobbyQuest = true;
                    break;
                }
            }
        }

        Debug.Log($"[QuestSpawnManager] Quest {nextQuest} -> Spawn Point: {spawnPointName}, Scene: {(isLobbyQuest ? lobbySceneName : worldSceneName)}");

        if (isLobbyQuest)
        {
            GoToLobby(spawnPointName);
        }
        else
        {
            GoToWorld(spawnPointName);
        }
    }

    public void GoToLobby(string spawnPoint = null)
    {
        StartCoroutine(TransitionRoutine(lobbySceneName, spawnPoint));
    }

    public void GoToWorld(string spawnPoint = null)
    {
        StartCoroutine(TransitionRoutine(worldSceneName, spawnPoint));
    }

    private IEnumerator TransitionRoutine(string sceneName, string spawnPoint)
    {
        // 1. Show Loading Screen immediately
        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ShowLoadingScreen("Loading Environment...");
        }

        // 3. Perform the actual transition (Disable fade for Loading Screen transitions)
        if (SceneTransitionManager.Instance != null)
        {
            // useFade = false because we are using a Loading Screen
            SceneTransitionManager.Instance.TransitionToScene(sceneName, spawnPoint, false);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }

        yield return null;
    }
}

