using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class UserAPI : MonoBehaviour
{
    public static UserAPI Instance { get; private set; }

    private string RegisterUrl => ApiConfig.AuthRegister;
    private string CreateUserUrl => ApiConfig.Users;
    private string LoginUrl => ApiConfig.AuthLogin;

    [System.Serializable]
    public class RegisterData
    {
        public string full_name;
        public string username;
        public string email;
        public string password;
    }

    [System.Serializable]
    public class UserData
    {
        public int id;              // Added to catch user.id
        public string full_name;
        public string username;
        public string email;
        public string phone;
        public string timezone;
        public string discovery_source;
        public string referral_code;
        public string referred_by;
        public string password;
    }

    [System.Serializable]
    public class LoginData
    {
        public string login;
        public string password;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public string message;
        public int userId;          // For some endpoints
        public int user_id;         // Added to catch snake_case user_id
        public int id;              // To catch root-level id
        public string token;
        public UserData user;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ====================== CREATE FULL USER ======================
    public void CreateFullUser(UserData data, System.Action<ApiResponse> onSuccess, System.Action<string> onError)
    {
        if (ApiConfig.UseLocalDemoMode)
        {
            HandleDemoCreateUser(data, onSuccess);
            return;
        }

        Debug.Log("🚀 Sending full user data to backend...");
        StartCoroutine(PostRequest(CreateUserUrl, data, onSuccess, onError));
    }

    // ====================== LOGIN ======================
    public void Login(string loginInput, string password, System.Action<ApiResponse> onSuccess, System.Action<string> onError)
    {
        if (ApiConfig.UseLocalDemoMode)
        {
            HandleDemoLogin(loginInput, password, onSuccess, onError);
            return;
        }

        LoginData data = new LoginData
        {
            login = loginInput.Trim(),
            password = password
        };

        Debug.Log($"🔑 Attempting login with: {loginInput}");
        StartCoroutine(PostRequest(LoginUrl, data, onSuccess, onError));
    }

    // ====================== UPDATE AVATAR ======================
    public void UpdateUserAvatar(int userId, int avatarIndex, System.Action<bool> onComplete)
    {
        if (ApiConfig.UseLocalDemoMode)
        {
            Debug.Log($"💾 [DEMO MODE] Avatar index {avatarIndex} saved for user {userId}");
            onComplete?.Invoke(true);
            return;
        }

        StartCoroutine(UpdateAvatarRoutine(userId, avatarIndex, onComplete));
    }

    private IEnumerator UpdateAvatarRoutine(int userId, int avatarIndex, System.Action<bool> onComplete)
    {
        string url = $"{ApiConfig.Users}/{userId}/avatar";
        
        // Use a JSON object for extensibility
        string json = $"{{\"avatar_index\": {avatarIndex}, \"hair_color_index\": 0, \"hair_style_index\": 0}}";
        
        Debug.Log($"📤 [AVATAR SYNC] Sending to: {url}");
        Debug.Log($"📤 [AVATAR SYNC] Data: {json}");

        // Use a more robust way to send PUT with JSON body
        UnityWebRequest request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ [AVATAR SYNC] Success!");
            onComplete?.Invoke(true);
        }
        else
        {
            // Log the full details for debugging
            string errorDetail = request.downloadHandler != null ? request.downloadHandler.text : "No response body";
            Debug.LogError($"❌ [AVATAR SYNC] Failed: {request.responseCode} {request.error} | Response: {errorDetail}");
            onComplete?.Invoke(false);
        }

        request.Dispose();
    }

    // ====================== DEMO MODE HELPERS ======================
    private void HandleDemoCreateUser(UserData data, System.Action<ApiResponse> onSuccess)
    {
        string json = JsonUtility.ToJson(data);
        
        // Save multiple users for demo login testing? 
        // For now, let's just save the current one with its username as a key
        PlayerPrefs.SetString("DemoUser_" + data.username, json);
        PlayerPrefs.SetString("DemoUser_" + data.email, json); // Also save by email for flexibility
        
        // Also set the last registered user as the current one
        PlayerPrefs.SetString("DemoUserData", json);
        PlayerPrefs.SetInt("CurrentUserId", 999);
        PlayerPrefs.Save();

        Debug.Log($"💾 [DEMO MODE] User {data.username} registered locally in PlayerPrefs");

        var response = new ApiResponse
        {
            message = "Demo registration successful",
            userId = 999
        };
        onSuccess?.Invoke(response);
    }

    private void HandleDemoLogin(string loginInput, string password, System.Action<ApiResponse> onSuccess, System.Action<string> onError)
    {
        // Try to find the user by username or email
        string savedJson = PlayerPrefs.GetString("DemoUser_" + loginInput, "");
        
        if (string.IsNullOrEmpty(savedJson))
        {
            Debug.LogWarning($"💾 [DEMO MODE] Login failed: User {loginInput} not found.");
            onError?.Invoke("Invalid email/username or password.");
            return;
        }

        UserData savedData = JsonUtility.FromJson<UserData>(savedJson);

        // Verify password (in demo mode, we just check if it matches)
        if (savedData.password != password)
        {
            Debug.LogWarning($"💾 [DEMO MODE] Login failed: Wrong password for user {loginInput}.");
            onError?.Invoke("Invalid email/username or password.");
            return;
        }

        Debug.Log($"💾 [DEMO MODE] Login successful for user: {loginInput}");

        var response = new ApiResponse
        {
            message = "Demo login successful",
            userId = 999, // Static ID for demo
            user = savedData
        };
        
        // Set as current user
        PlayerPrefs.SetInt("CurrentUserId", 999);
        PlayerPrefs.SetString("DemoUserData", savedJson);
        PlayerPrefs.Save();
        
        onSuccess?.Invoke(response);
    }

    // ====================== ORIGINAL POST REQUEST (Unchanged) ======================
    private IEnumerator PostRequest<T>(string url, T data, System.Action<ApiResponse> onSuccess, System.Action<string> onError)
    {
        string json = JsonUtility.ToJson(data);
        Debug.Log($"📤 POST → {url}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            string responseText = request.downloadHandler.text;

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ Success: {responseText}");
                ApiResponse response = JsonUtility.FromJson<ApiResponse>(responseText);
                onSuccess?.Invoke(response ?? new ApiResponse());
            }
            else
            {
                Debug.LogError($"❌ Failed: {request.responseCode} | {responseText}");
                string userFriendlyError = ExtractUserFriendlyError(responseText, request.responseCode);
                onError?.Invoke(userFriendlyError);
            }
        }
    }

    private string ExtractUserFriendlyError(string responseText, long statusCode)
    {
        if (string.IsNullOrEmpty(responseText))
            return "Connection error. Please check your internet.";

        try
        {
            var errorObj = JsonUtility.FromJson<ErrorResponse>(responseText);
            if (!string.IsNullOrEmpty(errorObj.error))
            {
                string err = errorObj.error.ToLower();
                if (err.Contains("already exists") || err.Contains("duplicate"))
                    return "Username or Email is already registered.\nPlease use a different one or try Login.";
                if (err.Contains("invalid") || err.Contains("not found"))
                    return "Invalid email/username or password.";
                return errorObj.error;
            }
        }
        catch { }

        if (statusCode == 400) return "Invalid information. Please check your details.";
        if (statusCode == 401) return "Invalid email/username or password.";

        return "Request failed. Please try again later.";
    }

    [System.Serializable]
    private class ErrorResponse
    {
        public string error;
    }
}