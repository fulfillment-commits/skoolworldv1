using System.Collections;
using System.Collections.Generic;
using ASAD_Multiplyer.Chat;
using ASAD_Multiplyer.Network;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogoutController : MonoBehaviour
{
    private const int RestartSceneIndex = 0;
    private const string PlayerPrefsUserId = "OnboardingUserId_Str";
    private const string PlayerPrefsUsername = "OnboardingUsername";
    private const string PlayerPrefsEmail = "OnboardingEmail";
    private const string PlayerPrefsRememberMe = "OnboardingRememberMe";
    private const string PlayerPrefsRememberUntilUtc = "OnboardingRememberUntilUtc";
    private const string PlayerPrefsAvatarIndex = "OnboardingAvatarIndex";
    private const string PlayerPrefsAvatarSelectedPrefix = "OnboardingAvatarSelected_";

    private static readonly HashSet<string> AutoBindButtonNames = new HashSet<string>
    {
        "Logout",
        "LogoutButton",
        "LogOutButton",
        "SignOut",
        "SignOutButton"
    };

    private static readonly HashSet<string> PersistentSessionTypes = new HashSet<string>
    {
        "OnboardingManager",
        "OnboardingQuestManager",
        "QuestSpawnManager",
        "ScreenManager",
        "GamePlayUIManager",
        "GameSettings",
        "FirebaseRestAPI",
        "VideoPlayerManager",
        "SceneTransitionManager",
        "DynamicMessagePanel",
        "RewardScreenManager",
        "QuestInteractionController",
        "QuestPanelUI",
        "QuestWorldStateManager",
        "PUN_ChatManager",
        "PUN_NetworkManager",
        "PUN_SyncPlayer",
        "SceneReference",
        "FirebaseBackendImplementation",
        "LocalDemoBackendImplementation",
        "CustomAPIBackendImplementation"
    };

    public static LogoutController Instance { get; private set; }

    private bool isLoggingOut;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        if (Instance != null || FindObjectOfType<LogoutController>() != null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("LogoutController");
        controllerObject.AddComponent<LogoutController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        BindLogoutButtonsInLoadedScenes();
    }

    public void LogoutAndRestart()
    {
        if (isLoggingOut)
        {
            return;
        }

        StartCoroutine(LogoutAndRestartRoutine());
    }

    public void Logout()
    {
        LogoutAndRestart();
    }

    public void BindLogoutButtonsInLoadedScenes()
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (button == null || !button.gameObject.scene.IsValid() || !IsLogoutButton(button))
            {
                continue;
            }

            button.onClick.RemoveListener(LogoutAndRestart);
            button.onClick.AddListener(LogoutAndRestart);
        }
    }

    private IEnumerator LogoutAndRestartRoutine()
    {
        isLoggingOut = true;
        Debug.Log("[LogoutController] Logout started.");

        TryShowLoadingState();
        TryDisableLocalPlayerControl();

        PUN_ChatManager.Instance?.ShutdownForLogout();

        IBackendService backendService = BackendSettings.Instance.Service;
        backendService?.SetRememberMe(false);
        backendService?.Logout();

        ClearSessionPrefs();

        if (PUN_NetworkManager.nm != null)
        {
            PUN_NetworkManager.nm.ShutdownForLogout();
        }
        else if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AuthValues = null;
            PhotonNetwork.NickName = string.Empty;
            PhotonNetwork.Disconnect();
        }

        yield return WaitForPhotonDisconnect();

        BackendSettings.ResetForLogout();
        DestroyPersistentSessionObjects();

        SceneManager.LoadScene(RestartSceneIndex);
        yield return null;

        isLoggingOut = false;
        BindLogoutButtonsInLoadedScenes();
        Debug.Log("[LogoutController] Logout completed.");
    }

    private static IEnumerator WaitForPhotonDisconnect()
    {
        float timeoutAt = Time.realtimeSinceStartup + 3f;
        while (PhotonNetwork.IsConnected && Time.realtimeSinceStartup < timeoutAt)
        {
            yield return null;
        }
    }

    private static void ClearSessionPrefs()
    {
        string previousUserId = PlayerPrefs.GetString(PlayerPrefsUserId, "");

        PlayerPrefs.DeleteKey(PlayerPrefsUserId);
        PlayerPrefs.DeleteKey(PlayerPrefsUsername);
        PlayerPrefs.DeleteKey(PlayerPrefsEmail);
        PlayerPrefs.DeleteKey(PlayerPrefsRememberMe);
        PlayerPrefs.DeleteKey(PlayerPrefsRememberUntilUtc);
        PlayerPrefs.DeleteKey(PlayerPrefsAvatarIndex);

        if (!string.IsNullOrEmpty(previousUserId))
        {
            PlayerPrefs.DeleteKey(PlayerPrefsAvatarSelectedPrefix + previousUserId);
        }

        PlayerPrefs.Save();
    }

    private static void TryShowLoadingState()
    {
        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ShowLoadingScreen("Signing out...");
        }
    }

    private static void TryDisableLocalPlayerControl()
    {
        if (PUN_NetworkManager.nm == null || PUN_NetworkManager.nm.myPlayer == null)
        {
            return;
        }

        ASAD_Multiplyer.PlayerController.PUN_SyncPlayer syncPlayer =
            PUN_NetworkManager.nm.myPlayer.GetComponent<ASAD_Multiplyer.PlayerController.PUN_SyncPlayer>();
        if (syncPlayer != null)
        {
            syncPlayer.SetControl(false);
        }
    }

    private void DestroyPersistentSessionObjects()
    {
        HashSet<GameObject> objectsToDestroy = new HashSet<GameObject>();
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this || !behaviour.gameObject.scene.IsValid())
            {
                continue;
            }

            if (PersistentSessionTypes.Contains(behaviour.GetType().Name))
            {
                objectsToDestroy.Add(behaviour.gameObject);
            }
        }

        EventSystem[] eventSystems = Resources.FindObjectsOfTypeAll<EventSystem>();
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != null && eventSystem.gameObject.scene.IsValid())
            {
                objectsToDestroy.Add(eventSystem.gameObject);
            }
        }

        foreach (GameObject target in objectsToDestroy)
        {
            if (target != null && target != gameObject)
            {
                Destroy(target);
            }
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindLogoutButtonsInLoadedScenes();
    }

    private static bool IsLogoutButton(Button button)
    {
        if (AutoBindButtonNames.Contains(button.gameObject.name))
        {
            return true;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null || string.IsNullOrWhiteSpace(label.text))
        {
            return false;
        }

        string normalized = label.text.Trim().ToLowerInvariant();
        return normalized == "logout"
               || normalized == "log out"
               || normalized == "sign out"
               || normalized == "signout";
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
