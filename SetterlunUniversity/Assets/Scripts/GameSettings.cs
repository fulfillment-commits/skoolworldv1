using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private bool isLocal = true;
    [SerializeField] private string localUrl = "http://localhost:5000";
    [SerializeField] private string domainUrl = "https://yourdomain.com";

    public bool IsLocal => isLocal;
    public string BaseUrl => isLocal ? localUrl : domainUrl;

    [Header("Demo Mode")]
    [Tooltip("TRUE = Save everything locally in PlayerPrefs (Perfect for client demos)\nFALSE = Use real API calls to backend")]
    [SerializeField] private bool useLocalDemoMode = false;

    public bool UseLocalDemoMode => useLocalDemoMode;

    private const string PLAYERPREFS_DEMO_MODE_KEY = "UseLocalDemoMode";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void OnEnable()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey(PLAYERPREFS_DEMO_MODE_KEY))
        {
            useLocalDemoMode = PlayerPrefs.GetInt(PLAYERPREFS_DEMO_MODE_KEY) == 1;
        }
    }

    public void SetDemoMode(bool value)
    {
        useLocalDemoMode = value;
        PlayerPrefs.SetInt(PLAYERPREFS_DEMO_MODE_KEY, value ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[GameSettings] Demo Mode set to: {value}");
    }

    private void OnValidate()
    {
        // Assignment to ApiConfig is no longer needed as ApiConfig reads from here directly
    }
}