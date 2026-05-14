using System;
using System.Collections.Generic;
using UnityEngine;
using Invector.vCharacterController;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance { get; private set; }

    [Header("Screen References")]
    [SerializeField] private ScreenBase[] allScreens;

    private Dictionary<ScreenType, ScreenBase> screenDictionary = new Dictionary<ScreenType, ScreenBase>();
    private ScreenType currentScreen = ScreenType.None;
    private ScreenType previousScreen = ScreenType.None;

    public event Action<ScreenType> OnScreenChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeScreens();
    }

    private void InitializeScreens()
    {
        screenDictionary.Clear();

        foreach (var screen in allScreens)
        {
            if (screen == null) continue;

            if (screenDictionary.ContainsKey(screen.ScreenType))
            {
                Debug.LogWarning($"Duplicate screen type: {screen.ScreenType}");
                continue;
            }

            screenDictionary.Add(screen.ScreenType, screen);
            screen.gameObject.SetActive(false);
        }

        // Start with Welcome or Login
        ShowScreen(ScreenType.Welcome);
        UpdatePlayerInputState();
    }

    public void ShowScreen(ScreenType screenType)
    {
        if (screenType == currentScreen) return;

        // Hide current
        if (currentScreen != ScreenType.None && screenDictionary.TryGetValue(currentScreen, out ScreenBase current))
        {
            current.Hide();
        }

        if((screenType==ScreenType.Course_BusinessModel || screenType==ScreenType.Course_BusinessPortal) && currentScreen != ScreenType.Course_BusinessModel && currentScreen != ScreenType.Course_BusinessPortal)
            previousScreen = currentScreen;

        // Show new
        if (screenDictionary.TryGetValue(screenType, out ScreenBase nextScreen))
        {
            nextScreen.Show();
            currentScreen = screenType;

            Debug.Log($"[ScreenManager] Switched to: {screenType}");
            OnScreenChanged?.Invoke(screenType);
            UpdatePlayerInputState();
        }
        else
        {
            Debug.LogError($"[ScreenManager] Screen not found: {screenType}");
        }
    }

    public void HideCurrentScreen()
    {
        if (currentScreen != ScreenType.None && screenDictionary.TryGetValue(currentScreen, out ScreenBase current))
        {
            current.Hide();
            currentScreen = ScreenType.None;
            UpdatePlayerInputState();
        }
    }

    /// <summary>Hide the current screen and restore the one shown before it.</summary>
    public void GoBack()
    {
        if (previousScreen != ScreenType.None)
            ShowScreen(previousScreen);
        else
            HideCurrentScreen();
    }

    // ====================== HELPER METHODS ======================
    public void ShowScreenDirectly(ScreenType screenType) => ShowScreen(screenType);
    public void GoToLogin() => ShowScreen(ScreenType.Login);
    public void GoToSettings() => ShowScreen(ScreenType.Settings);
    public void GoToMainWorld() => ShowScreen(ScreenType.MainWorld);

    public LoadingScreen GetLoadingScreen()
    {
        if (screenDictionary.TryGetValue(ScreenType.Loading, out ScreenBase screen))
        {
            return screen as LoadingScreen;
        }
        return null;
    }

    public void ShowLoadingScreen(string status = "Loading...")
    {
        LoadingScreen loading = GetLoadingScreen();
        if (loading != null)
        {
            loading.StartLoading(null, status);
        }
        else
        {
            ShowScreen(ScreenType.Loading);
        }
    }

    public void HideLoadingScreen(System.Action onComplete = null)
    {
        LoadingScreen loading = GetLoadingScreen();
        if (loading != null)
        {
            // Instead of restarting a loading bar, we just hide it after the "Complete!" delay
            loading.Hide();
            ShowScreen(ScreenType.MainWorld);
            onComplete?.Invoke();
        }
        else
        {
            ShowScreen(ScreenType.MainWorld);
            onComplete?.Invoke();
        }
    }

    public void LoadSceneWithLoading(string sceneName, System.Action onComplete = null)
    {
        LoadingScreen loading = GetLoadingScreen();
        if (loading != null)
        {
            loading.StartLoading(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                onComplete?.Invoke();
            }, "Loading Scene...");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            onComplete?.Invoke();
        }
    }

    // Removed University, BasicInfo, AvatarSelection because they don't exist

    public void UpdatePlayerInputState()
    {
        bool isDynamicPanelActive = DynamicMessagePanel.Instance != null && 
                                   DynamicMessagePanel.Instance.gameObject.activeInHierarchy;

        bool shouldLock = (currentScreen != ScreenType.None && currentScreen != ScreenType.MainWorld) || 
                         isDynamicPanelActive;

        if (ReferencesManager.Instance != null && ReferencesManager.Instance.player != null)
        {
            var tpInput = ReferencesManager.Instance.player.GetComponent<vThirdPersonInput>();
            if (tpInput != null)
            {
                tpInput.SetLockAllInput(shouldLock);
            }
        }
        
        // Ensure cursor is ALWAYS visible and unlocked as per user request
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}