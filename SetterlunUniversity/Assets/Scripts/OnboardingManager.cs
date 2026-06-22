using System;
using System.Collections;
using System.Text;
using ASAD_Multiplyer.Network;
using ASAD_Multiplyer.PlayerController;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class OnboardingManager : MonoBehaviour
{
    public static OnboardingManager Instance { get; private set; }

    [Header("Screen Configuration")]
    [SerializeField] private ScreenType welcomeScreenType = ScreenType.Welcome;
    [SerializeField] private ScreenType basicInfoScreenType = ScreenType.FastRegister;
    [SerializeField] private ScreenType avatarScreenType = ScreenType.FastAvatar;

    private string currentUserId = "";
    private string currentUsername = "";
    private string currentEmail = "";

    private const string PLAYERPREFS_USER_ID = "OnboardingUserId_Str";
    private const string PLAYERPREFS_USERNAME = "OnboardingUsername";
    private const string PLAYERPREFS_EMAIL = "OnboardingEmail";
    private const string PLAYERPREFS_REMEMBER_ME = "OnboardingRememberMe";
    private const string PLAYERPREFS_REMEMBER_UNTIL_UTC = "OnboardingRememberUntilUtc";
    private const string PLAYERPREFS_AVATAR_INDEX = "OnboardingAvatarIndex";
    private const string PLAYERPREFS_AVATAR_SELECTED_PREFIX = "OnboardingAvatarSelected_";
    private const int REMEMBER_DAYS = 30;
    private Coroutine enterWorldWhenReadyRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSession();
    }

    private void LoadSession()
    {
        currentUserId = PlayerPrefs.GetString(PLAYERPREFS_USER_ID, "");
        currentUsername = PlayerPrefs.GetString(PLAYERPREFS_USERNAME, "");
        currentEmail = PlayerPrefs.GetString(PLAYERPREFS_EMAIL, "");
    }

    private void SaveSession()
    {
        PlayerPrefs.SetString(PLAYERPREFS_USER_ID, currentUserId);
        PlayerPrefs.SetString(PLAYERPREFS_USERNAME, currentUsername);
        PlayerPrefs.SetString(PLAYERPREFS_EMAIL, currentEmail);
        PlayerPrefs.Save();
    }

    private void Start()
    {
        if (ScreenManager.Instance != null)
        {
            if (ShouldAttemptRememberedLogin())
            {
                IBackendService service = BackendSettings.Instance.Service;
                if (service != null)
                {
                    ScreenManager.Instance.ShowLoadingScreen("Restoring session...");
                    service.TryAutoLogin(HandleAutoLoginSuccess, HandleAutoLoginFailed);
                    return;
                }

                Debug.LogWarning("[OnboardingManager] Backend service is not ready, cannot restore remembered session.");
            }

            ScreenManager.Instance.ShowScreen(welcomeScreenType);
        }
    }

    public void Initialize(string userId, string username = "", string email = "")
    {
        currentUserId = userId;
        currentUsername = username;
        currentEmail = email;

        SaveSession();

        // Sync with Backend Service
        if (BackendSettings.Instance != null && BackendSettings.Instance.Service != null)
        {
            BackendSettings.Instance.Service.SetUserId(userId);
        }

        Debug.Log($"Initialized OnboardingManager for User ID: {userId}, Username: {username}");
        
        InitializeQuestManager();
        
        if (PUN_NetworkManager.nm != null)
        {
            PUN_NetworkManager.nm.ConnetNow();
        }
        else
        {
            Debug.LogWarning("[OnboardingManager] PUN_NetworkManager is not ready, skipping immediate Photon connect.");
        }
    }

    public void SetRememberMe(bool rememberMe)
    {
        if (rememberMe)
        {
            PlayerPrefs.SetInt(PLAYERPREFS_REMEMBER_ME, 1);
            PlayerPrefs.SetString(PLAYERPREFS_REMEMBER_UNTIL_UTC, DateTime.UtcNow.AddDays(REMEMBER_DAYS).ToString("O"));
        }
        else
        {
            PlayerPrefs.SetInt(PLAYERPREFS_REMEMBER_ME, 0);
            PlayerPrefs.DeleteKey(PLAYERPREFS_REMEMBER_UNTIL_UTC);
        }

        PlayerPrefs.Save();
    }

    private bool ShouldAttemptRememberedLogin()
    {
        if (PlayerPrefs.GetInt(PLAYERPREFS_REMEMBER_ME, 0) != 1)
        {
            return false;
        }

        string rememberUntil = PlayerPrefs.GetString(PLAYERPREFS_REMEMBER_UNTIL_UTC, "");
        if (string.IsNullOrEmpty(rememberUntil) || !DateTime.TryParse(rememberUntil, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime expiryUtc))
        {
            ClearRememberedLogin();
            return false;
        }

        if (DateTime.UtcNow > expiryUtc.ToUniversalTime())
        {
            ClearRememberedLogin();
            BackendSettings.Instance.Service?.Logout();
            return false;
        }

        return true;
    }

    private void HandleAutoLoginSuccess(BackendResponse response)
    {
        if (response == null || string.IsNullOrEmpty(response.userId))
        {
            HandleAutoLoginFailed("Auto-login response did not include a user id.");
            return;
        }

        Initialize(response.userId, response.username, response.email);
        ContinueAfterLogin(response);
    }

    private void HandleAutoLoginFailed(string error)
    {
        Debug.Log($"[OnboardingManager] Auto-login skipped: {error}");
        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ShowScreen(welcomeScreenType);
        }
    }

    private static void ClearRememberedLogin()
    {
        PlayerPrefs.SetInt(PLAYERPREFS_REMEMBER_ME, 0);
        PlayerPrefs.DeleteKey(PLAYERPREFS_REMEMBER_UNTIL_UTC);
        PlayerPrefs.Save();
    }

    public void StartJourney()
    {
        if (ScreenManager.Instance != null)
        {
            // Always default to FastRegister when starting the journey
            Debug.Log("[OnboardingManager] StartJourney - Showing FastRegisterScreen");
            ScreenManager.Instance.ShowScreen(ScreenType.FastRegister);
        }
        else
        {
            Debug.LogError("[OnboardingManager] ScreenManager.Instance is null!");
        }
    }

    public void ContinueToAvatar()
    {
        ScreenManager.Instance.ShowScreen(avatarScreenType);
    }

    public void ContinueAfterLogin(BackendResponse response)
    {
        SaveAvatarFromBackend(response);

        if (HasSelectedAvatar(response))
        {
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.ShowLoadingScreen("Preparing world...");
            }

            EnterWorldWhenNetworkPlayerReady();
            return;
        }

        ScreenManager.Instance.ShowScreen(avatarScreenType);
    }

    public void MarkAvatarSelected(int avatarIndex)
    {
        PlayerPrefs.SetInt(PLAYERPREFS_AVATAR_INDEX, avatarIndex);

        if (!string.IsNullOrEmpty(currentUserId))
        {
            PlayerPrefs.SetInt(GetAvatarSelectedPrefsKey(currentUserId), 1);
        }

        PlayerPrefs.Save();
    }

    public void EnterWorldWhenNetworkPlayerReady()
    {
        if (enterWorldWhenReadyRoutine != null)
        {
            StopCoroutine(enterWorldWhenReadyRoutine);
        }

        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ShowLoadingScreen("Connecting player...");
        }

        enterWorldWhenReadyRoutine = StartCoroutine(EnterWorldWhenNetworkPlayerReadyRoutine());
    }

    private IEnumerator EnterWorldWhenNetworkPlayerReadyRoutine()
    {
        const float timeout = 30f;
        float elapsed = 0f;
        LoadingScreen loading = ScreenManager.Instance != null ? ScreenManager.Instance.GetLoadingScreen() : null;

        while (elapsed < timeout)
        {
            if (PUN_NetworkManager.nm != null && PUN_NetworkManager.nm.myPlayer != null)
            {
                ApplySavedAvatarToLocalPlayer();
                enterWorldWhenReadyRoutine = null;
                EnterWorld();
                yield break;
            }

            if (loading != null)
            {
                loading.SetStatus("Connecting player...");
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        enterWorldWhenReadyRoutine = null;
        Debug.LogWarning("[OnboardingManager] Timed out waiting for Photon player before entering world.");
        if (loading != null)
        {
            loading.SetStatus("Network player is still connecting. Please check your connection.");
        }
    }

    private void ApplySavedAvatarToLocalPlayer()
    {
        if (PUN_NetworkManager.nm == null || PUN_NetworkManager.nm.myPlayer == null)
        {
            return;
        }

        PUN_SyncPlayer syncPlayer = PUN_NetworkManager.nm.myPlayer.GetComponent<PUN_SyncPlayer>();
        if (syncPlayer != null)
        {
            syncPlayer.LoadOutfit();
        }
    }

    private void SaveAvatarFromBackend(BackendResponse response)
    {
        if (response == null)
        {
            return;
        }

        if (response.avatar_index >= 0)
        {
            PlayerPrefs.SetInt(PLAYERPREFS_AVATAR_INDEX, response.avatar_index);
        }

        if (!string.IsNullOrEmpty(response.userId) && HasSelectedAvatar(response))
        {
            PlayerPrefs.SetInt(GetAvatarSelectedPrefsKey(response.userId), 1);
        }

        PlayerPrefs.Save();
    }

    private bool HasSelectedAvatar(BackendResponse response)
    {
        if (response == null)
        {
            return false;
        }

        if (response.avatar_selected || response.avatar_index > 0)
        {
            return true;
        }

        return !string.IsNullOrEmpty(response.userId)
               && PlayerPrefs.GetInt(GetAvatarSelectedPrefsKey(response.userId), 0) == 1;
    }

    private static string GetAvatarSelectedPrefsKey(string userId)
    {
        return PLAYERPREFS_AVATAR_SELECTED_PREFIX + userId;
    }

    public void EnterWorld()
    {
        if (QuestSpawnManager.Instance != null)
        {
            TriggerMagicMoment(() =>
            {
                QuestSpawnManager.Instance.SpawnForNextQuest();
            });
        }
        else
        {
            // Fallback to old method if QuestSpawnManager is missing
            TriggerMagicMoment(() =>
            {
                StartCoroutine(LoadWorldAsync());
            });
        }
    }

    private IEnumerator LoadWorldAsync()
    {
        LoadingScreen loading = ScreenManager.Instance.GetLoadingScreen();
        if (loading != null)
        {
            // Start loading with a message
            loading.StartLoading(null, "Entering World...");
        }

        // Use SceneTransitionManager if available
        if (SceneTransitionManager.Instance != null && QuestSpawnManager.Instance != null)
        {
            int nextQuest = OnboardingQuestManager.Instance != null ? OnboardingQuestManager.Instance.GetNextIncompleteQuest() : 1;
            string spawnPointName = "";
            
            if (nextQuest == -1)
            {
                spawnPointName = "Spawn_Final";
            }
            else
            {
                int index = nextQuest - 1;
                if (index >= 0 && index < QuestSpawnManager.Instance.questSpawnPoints.Length)
                {
                    spawnPointName = QuestSpawnManager.Instance.questSpawnPoints[index];
                }
            }

            SceneTransitionManager.Instance.TransitionToScene("WorldScene", spawnPointName, false);
            yield break;
        }

        // Load the scene asynchronously (Original Fallback)
        AsyncOperation asyncOp = SceneManager.LoadSceneAsync("WorldScene");
        asyncOp.allowSceneActivation = true;

        // Wait until the scene is fully loaded
        while (!asyncOp.isDone)
        {
            yield return null;
        }

        // Scene is loaded, wait a tiny bit for objects to initialize
        yield return new WaitForSeconds(0.5f);

        // Hide loading screen only AFTER environment is ready
        if (loading != null)
        {
            loading.Hide();
        }

        // Show Main World UI
        ScreenManager.Instance.ShowScreen(ScreenType.MainWorld);

        // Sync with PlayerSpawnManager if it exists
        // if (PlayerSpawnManager.Instance != null && QuestSpawnManager.Instance != null)
        // {
        //     int nextQuest = OnboardingQuestManager.Instance != null ? OnboardingQuestManager.Instance.GetNextIncompleteQuest() : 1;
        //     string spawnPointName = "";
        //     
        //     if (nextQuest == -1)
        //     {
        //         spawnPointName = "Spawn_Final";
        //     }
        //     else
        //     {
        //         int index = nextQuest - 1;
        //         if (index >= 0 && index < QuestSpawnManager.Instance.questSpawnPoints.Length)
        //         {
        //             spawnPointName = QuestSpawnManager.Instance.questSpawnPoints[index];
        //         }
        //     }
        //     
        //     PlayerSpawnManager.Instance.CheckForSpawnPoint(spawnPointName);
        // }

        // Refresh World State based on quest completion
        if (QuestWorldStateManager.Instance != null)
        {
            QuestWorldStateManager.Instance.RefreshWorldState();
        }

        // Auto-start the next quest screen if not completed
        StartCoroutine(AutoShowNextQuestRoutine());

        Debug.Log("[OnboardingManager] World Loaded. Quest progress synced from Firebase.");
    }

    private IEnumerator AutoShowNextQuestRoutine()
    {
        // 1. Wait for world to settle
        yield return new WaitForSeconds(1.5f);

        // 2. Wait for Quest progress to be synced from backend
        if (OnboardingQuestManager.Instance != null)
        {
            float timeout = 5f;
            float elapsed = 0f;
            while (!OnboardingQuestManager.Instance.IsInitialSyncComplete && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (OnboardingQuestManager.Instance != null && ScreenManager.Instance != null)
        {
            // ONLY auto-show Quest 1. All other quests must be triggered by player interaction.
            if (!OnboardingQuestManager.Instance.IsQuestCompleted(1))
            {
                Debug.Log("🚀 Auto-starting Quest 1 (Tour)");
                ScreenManager.Instance.ShowScreen(ScreenType.Quest_Tour);
            }
            else
            {
                Debug.Log("✅ Quest 1 already completed. Waiting for player interaction for next quests.");
            }
        }
    }

    private ScreenType GetScreenForQuest(int questNumber)
    {
        return questNumber switch
        {
            1 => ScreenType.Quest_Tour,
            2 => ScreenType.Quest_BrickClaim,
            3 => ScreenType.Quest_PersonalProfile,
            4 => ScreenType.Quest_BusinessProfile,
            5 => ScreenType.Quest_LeadsSales,
            6 => ScreenType.Quest_Operations,
            7 => ScreenType.Quest_Authority,
            8 => ScreenType.Quest_Struggles,
            9 => ScreenType.Quest_Goals,
            10 => ScreenType.Quest_SelfAwareness,
            _ => ScreenType.MainWorld
        };
    }

    public void CompleteFullOnboarding()
    {
        Debug.Log("🎉 Onboarding Complete! Triggering congratulatory sequence...");
        
        if (DynamicMessagePanel.Instance != null)
        {
            DynamicMessagePanel.Instance.Show("Congratulations, you have completed all the Quests", () => 
            {
                // Deactivate Quest Panel if visible
                if (QuestPanelUI.Instance != null)
                {
                    QuestPanelUI.Instance.gameObject.SetActive(false);
                }

                // Show the final reward screen
                if (ScreenManager.Instance != null)
                {
                    ScreenManager.Instance.ShowScreen(ScreenType.OnboardingComplete);
                }
            });
        }
        else
        {
            // Fallback if panel is missing
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.ShowScreen(ScreenType.OnboardingComplete);
            }
        }
    }

    public string GetUserStage()
    {
        // Extract revenue from Quest 4 data
        if (OnboardingQuestManager.Instance == null) return "Early Builder";
        string q4Data = OnboardingQuestManager.Instance.GetQuestData(4);
        if (string.IsNullOrEmpty(q4Data)) return "Early Builder";

        try {
            // Quest 4 uses clean JSON with monthly_revenue field
            var data = JsonUtility.FromJson<Quest4RevenueData>(q4Data);
            string rev = data.monthly_revenue;
            if (rev == "$0" || rev == "$1–$5K") return "Early Builder";
            if (rev == "$5–$10K" || rev == "$10–$25K") return "Growth Phase";
            if (rev == "$25–$50K" || rev == "$50–$100K") return "Scaling Expert";
            if (rev == "$100K+") return "Market Leader";
        } catch { }

        return "Early Builder";
    }

    [System.Serializable]
    private class Quest4RevenueData { public string monthly_revenue; }

    public string GetKeyWeakness()
    {
        // Extract struggles from Quest 8 data
        if (OnboardingQuestManager.Instance == null) return "Lack of Roadmap";
        string q8Data = OnboardingQuestManager.Instance.GetQuestData(8);
        if (string.IsNullOrEmpty(q8Data)) return "Lack of Roadmap";

        try {
            // Quest 8 uses clean JSON with biggest_struggles array
            var data = JsonUtility.FromJson<QuestStrugglesData>(q8Data);
            if (data.biggest_struggles != null && data.biggest_struggles.Length > 0)
                return data.biggest_struggles[0];
        } catch { }

        return "Lack of Roadmap";
    }

    [System.Serializable]
    private class QuestStrugglesData { public string[] biggest_struggles; }

    public void ShowNextIncompleteQuest()
    {
        if (OnboardingQuestManager.Instance == null) return;

        // ONLY auto-show if Quest 1 is not completed.
        // All other quests (2-10) are trigger-based button clicks in the world.
        if (!OnboardingQuestManager.Instance.IsQuestCompleted(1))
        {
            Debug.Log("🚀 Showing Quest 1 (Tour) automatically.");
            ScreenManager.Instance.ShowScreen(ScreenType.Quest_Tour);
        }
    }

    private void TriggerMagicMoment(System.Action onComplete)
    {
        Debug.Log("🌟 MAGIC MOMENT - Entering Setterlun University World!");
        StartCoroutine(MagicMomentRoutine(onComplete));
    }

    private IEnumerator MagicMomentRoutine(System.Action onComplete)
    {
        yield return new WaitForSeconds(0.85f);
        onComplete?.Invoke();
    }

    public void StartOnboardingStep1(string fullName, string username, string email,
        string phone, string password, string discoverySource, string referralCode, Action<bool, string> callback)
    {
        var userData = new UserData
        {
            full_name = fullName,
            username = username,
            email = email,
            phone = phone,
            password = password
        };

        BackendSettings.Instance.Service.Register(userData,
            onSuccess: (response) =>
            {
                if (response != null && !string.IsNullOrEmpty(response.userId))
                {
                    currentUserId = response.userId;
                    currentUsername = username;
                    currentEmail = email;

                    SaveSession();
                    InitializeQuestManager();

                    Debug.Log($"✅ Step 1 Complete - User Created! ID: {currentUserId}");
                    callback?.Invoke(true, "User created successfully");
                }
                else
                {
                    callback?.Invoke(false, "Failed to get user ID from response");
                }
            },
            onError: (error) =>
            {
                Debug.LogError("❌ Step 1 Failed: " + error);
                callback?.Invoke(false, error);
            }
        );
    }

    private void InitializeQuestManager()
    {
        if (OnboardingQuestManager.Instance != null)
        {
            OnboardingQuestManager.Instance.Initialize(currentUserId, currentUsername);
        }
    }

    public void CreatePersonalProfile(PersonalProfileData profileData, Action<bool, string> callback)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            callback?.Invoke(false, "User not created yet. Complete Step 1 first.");
            return;
        }

        // Convert to the generic UserData structure used by the interface
        UserData userData = new UserData {
            id = currentUserId,
            full_name = profileData.full_name,
            email = profileData.email,
            phone = profileData.phone,
            username = currentUsername // Ensure username is preserved
        };
        
        // Use CreatePersonalProfile instead of Register for updates
        BackendSettings.Instance.Service.CreatePersonalProfile(userData, (success, msg) => {
            if (success)
            {
                // Also save to Quest system
                OnboardingQuestManager.Instance?.CompleteQuest(3, JsonUtility.ToJson(profileData));
                
                // Update local session if name changed
                if (!string.IsNullOrEmpty(profileData.full_name))
                {
                    currentUsername = profileData.full_name; // Or keep original username
                    SaveSession();
                }

                callback?.Invoke(true, "Personal profile synced");
            }
            else
            {
                callback?.Invoke(false, msg);
            }
        });
    }

    public void CompleteQuest(int questNumber, string dataJson, Action<bool, string> callback)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            callback?.Invoke(false, "User not created yet.");
            return;
        }

        // 1. Sync with Backend/Firebase
        BackendSettings.Instance.Service.CompleteQuest(questNumber, dataJson, (success, msg) => 
        {
            if (success)
            {
                // 2. Also save to local Quest Manager
                if (OnboardingQuestManager.Instance != null)
                {
                    OnboardingQuestManager.Instance.CompleteQuest(questNumber, dataJson);
                }
                callback?.Invoke(true, "Quest completed and synced");
            }
            else
            {
                callback?.Invoke(false, msg);
            }
        });
    }

    public void CreateBusinessProfile(BusinessProfileData businessData, Action<bool, string> callback)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            callback?.Invoke(false, "User not created yet.");
            return;
        }

        businessData.user_id = currentUserId;

        // For now, Business Profile is treated as Quest 4
        string json = JsonUtility.ToJson(businessData);
        BackendSettings.Instance.Service.CompleteQuest(4, json, callback);
    }

    public void CreateBrick(string nameOnBrick, string businessName, string message,
        Action<bool, string> callback)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            callback?.Invoke(false, "User not created yet.");
            return;
        }

        var brickData = new BrickData
        {
            user_id = currentUserId,
            name_on_brick = nameOnBrick,
            business_name = businessName,
            message = message
        };
        
        string json = JsonUtility.ToJson(brickData);
        BackendSettings.Instance.Service.CompleteQuest(2, json, callback);
    }

    public void SaveQuestData(int questNumber, string dataJson, Action<bool, string> callback)
    {
        if (OnboardingQuestManager.Instance != null)
        {
            OnboardingQuestManager.Instance.CompleteQuest(questNumber, dataJson);
            callback?.Invoke(true, "Quest data saved");
        }
        else
        {
            callback?.Invoke(false, "Quest manager not found");
        }
    }

    public string CurrentUserId => currentUserId;
    public string CurrentUsername => currentUsername;
    public string CurrentEmail => currentEmail;

    public void SetUserId(string userId)
    {
        currentUserId = userId;
    }

    [System.Serializable]
    public class PersonalProfileData
    {
        public string user_id;
        public string full_name;
        public string email;
        public string timezone;
        public string city;
        public string country;
        public string bio;
        public string website;
        public string phone;
        public string visibility; // "public" or "private"
        public string[] skills;
        public string years_in_business;
    }

    [System.Serializable]
    public class BusinessProfileData
    {
        public string user_id;
        public string business_name;
        public string business_website;
        public string monthly_revenue;
        public string business_type;
        public string[] products_services;
        public string[] lead_sources;
        public string[] sales_issues;
        public string sales_process_status;
        public string[] fulfillment_challenges;
        public string[] tools_used;
        public string authority_level;
        public bool active_authority_building;
        public string[] biggest_struggles;
        public string primary_goal_90_day;
        public string time_commitment;
        public string self_awareness;
    }

    [System.Serializable]
    private class BrickData
    {
        public string user_id;
        public string name_on_brick;
        public string business_name;
        public string message;
    }
}
