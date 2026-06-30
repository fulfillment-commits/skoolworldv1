using UnityEngine;

public class BackendSettings
{
    private static BackendSettings instance;
    public static BackendSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new BackendSettings();
                instance.Initialize();
            }
            return instance;
        }
    }

    private IBackendService currentService;
    private BackendConfig config;
    private GameObject runner;

    private void Initialize()
    {
        config = Resources.Load<BackendConfig>("BackendConfig");

        if (config == null)
        {
            Debug.LogError("[BackendSettings] BackendConfig not found in Resources. Open Setterlun -> Backend Settings to create it.");
            return;
        }

        runner = new GameObject("BackendRunner");
        Object.DontDestroyOnLoad(runner);
        runner.hideFlags = HideFlags.HideAndDontSave;

        if (config.activeBackend == BackendType.LocalDemo || ApiConfig.UseLocalDemoMode)
        {
            currentService = runner.AddComponent<LocalDemoBackendImplementation>();
            Debug.Log("[BackendSettings] Active Backend: Local Demo (PlayerPrefs)");
        }
        else if (config.activeBackend == BackendType.Firebase)
        {
            var fb = runner.AddComponent<FirebaseBackendImplementation>();
            fb.Setup(config.GetFirebaseConfigJson());
            currentService = fb;
            Debug.Log("[BackendSettings] Active Backend: Firebase");
        }
        else if (config.activeBackend == BackendType.PortalBridge)
        {
            currentService = runner.AddComponent<PortalBridgeBackendImplementation>();
            Debug.Log("[BackendSettings] Active Backend: Portal Bridge");
        }
        else
        {
            currentService = runner.AddComponent<CustomAPIBackendImplementation>();
            Debug.Log("[BackendSettings] Active Backend: Custom API");
        }
    }

    public IBackendService Service => currentService;

    public static void ResetForLogout()
    {
        if (instance == null)
        {
            return;
        }

        instance.currentService = null;
        instance.config = null;

        if (instance.runner != null)
        {
            Object.Destroy(instance.runner);
            instance.runner = null;
        }

        instance = null;
    }
}
