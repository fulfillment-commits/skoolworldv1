using UnityEngine;
using UnityEditor;

public class BackendSettingsWindow : EditorWindow
{
    private BackendConfig config;

    [MenuItem("Setterlun/Backend Settings")]
    public static void ShowWindow()
    {
        GetWindow<BackendSettingsWindow>("Backend Settings");
    }

    private void OnEnable()
    {
        LoadConfig();
    }

    private void LoadConfig()
    {
        config = Resources.Load<BackendConfig>("BackendConfig");
        
        if (config == null)
        {
            // Try to find it in the project if not in Resources
            string[] guids = AssetDatabase.FindAssets("t:BackendConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                config = AssetDatabase.LoadAssetAtPath<BackendConfig>(path);
            }
            else
            {
                // Create it if it doesn't exist
                config = CreateInstance<BackendConfig>();
                if (!AssetDatabase.IsValidFolder("Assets/Modules/Core/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets/Modules/Core", "Resources");
                }
                AssetDatabase.CreateAsset(config, "Assets/Modules/Core/Resources/BackendConfig.asset");
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void OnGUI()
    {
        if (config == null) LoadConfig();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Backend Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        BackendType newBackend = (BackendType)EditorGUILayout.EnumPopup("Active Backend", config.activeBackend);
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(config, "Change Backend Type");
            config.activeBackend = newBackend;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ [Backend Settings] Switched to {newBackend}");
        }

        if (config.activeBackend == BackendType.Firebase)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Firebase Configuration", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            config.apiKey = EditorGUILayout.TextField("API Key", config.apiKey);
            config.authDomain = EditorGUILayout.TextField("Auth Domain", config.authDomain);
            config.projectId = EditorGUILayout.TextField("Project ID", config.projectId);
            config.storageBucket = EditorGUILayout.TextField("Storage Bucket", config.storageBucket);
            config.messagingSenderId = EditorGUILayout.TextField("Messaging Sender ID", config.messagingSenderId);
            config.appId = EditorGUILayout.TextField("App ID", config.appId);
            config.measurementId = EditorGUILayout.TextField("Measurement ID (Optional)", config.measurementId);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Update Firebase Credentials");
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.HelpBox("This setting controls whether the app uses Firebase or the Custom API for all data operations.", MessageType.Info);
        
        if (GUILayout.Button("Force Refresh", GUILayout.Height(30)))
        {
            LoadConfig();
        }
    }
}
