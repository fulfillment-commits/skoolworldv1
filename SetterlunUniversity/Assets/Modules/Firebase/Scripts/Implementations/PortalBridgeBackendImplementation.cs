using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class PortalBridgeBackendImplementation : MonoBehaviour, IBackendService
{
    [DllImport("__Internal")]
    private static extern void Portal_Request(string requestJson);

    [DllImport("__Internal")]
    private static extern void Portal_NotifyReady();

    [DllImport("__Internal")]
    private static extern void Portal_Logout();

    private const string SourceUnity = "setterlun-unity";
    private const string SourcePortal = "setterlun-portal";
    private const string MessageTypeRequest = "backend.request";
    private const string MessageTypeResponse = "backend.response";
    private const string MessageTypePortalInit = "portal.init";

    private const string PlayerPrefsUserId = "OnboardingUserId_Str";
    private const string PlayerPrefsUsername = "OnboardingUsername";
    private const string PlayerPrefsEmail = "OnboardingEmail";
    private const string PlayerPrefsRememberMe = "OnboardingRememberMe";
    private const string PlayerPrefsAvatarIndex = "OnboardingAvatarIndex";
    private const string PlayerPrefsAvatarSelectedPrefix = "OnboardingAvatarSelected_";

    private readonly Dictionary<string, PendingRequest> pendingRequests = new Dictionary<string, PendingRequest>();

    private BackendResponse cachedSession;
    private QuestProgressData[] cachedQuestProgress = new QuestProgressData[0];
    private string currentUserId = "";
    private bool hasPortalInit;

    private Action<BackendResponse> pendingAutoLoginSuccess;
    private Action<string> pendingAutoLoginError;

    private void Start()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        Portal_NotifyReady();
#endif
    }

    private sealed class PendingRequest
    {
        public Action<string> onSuccess;
        public Action<string> onError;
    }

    public void SetUserId(string userId)
    {
        currentUserId = userId ?? "";
        if (!string.IsNullOrEmpty(currentUserId))
        {
            PlayerPrefs.SetString(PlayerPrefsUserId, currentUserId);
            PlayerPrefs.Save();
        }
    }

    public void SetRememberMe(bool rememberMe)
    {
        PlayerPrefs.SetInt(PlayerPrefsRememberMe, rememberMe ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void TryAutoLogin(Action<BackendResponse> onSuccess, Action<string> onError)
    {
        if (cachedSession != null && !string.IsNullOrEmpty(cachedSession.userId))
        {
            onSuccess?.Invoke(cachedSession);
            return;
        }

        pendingAutoLoginSuccess = onSuccess;
        pendingAutoLoginError = onError;

        SendRequest("getSession", "{}", dataJson =>
        {
            BackendResponse response = ParseBackendResponse(dataJson);
            if (response == null || string.IsNullOrEmpty(response.userId))
            {
                onError?.Invoke("Portal session response did not include a user id.");
                return;
            }

            ApplySession(response);
            onSuccess?.Invoke(response);
        }, onError);
    }

    public void Login(string login, string password, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        if (cachedSession != null && !string.IsNullOrEmpty(cachedSession.userId))
        {
            onSuccess?.Invoke(cachedSession);
            return;
        }

        onError?.Invoke("Use the portal Supabase login before opening the Unity world.");
    }

    public void Register(UserData data, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        onError?.Invoke("Registration is handled by the React portal with Supabase Auth.");
    }

    public void Logout()
    {
        currentUserId = "";
        cachedSession = null;
        hasPortalInit = false;

        PlayerPrefs.DeleteKey(PlayerPrefsUserId);
        PlayerPrefs.DeleteKey(PlayerPrefsUsername);
        PlayerPrefs.DeleteKey(PlayerPrefsEmail);
        PlayerPrefs.Save();

#if !UNITY_EDITOR && UNITY_WEBGL
        Portal_Logout();
#endif
    }

    public void FirestoreSet(string path, string json, Action<bool, string> callback)
    {
        string payload = "{" +
            JsonField("path", path) + "," +
            JsonField("json", json) +
        "}";

        SendRequest("firestoreSet", payload,
            dataJson => callback?.Invoke(true, dataJson),
            error => callback?.Invoke(false, error));
    }

    public void FirestoreUpdate(string path, string json, Action<bool, string> callback)
    {
        string payload = "{" +
            JsonField("path", path) + "," +
            JsonField("json", json) +
        "}";

        SendRequest("firestoreUpdate", payload,
            dataJson => callback?.Invoke(true, dataJson),
            error => callback?.Invoke(false, error));
    }

    public void FirestoreGet(string path, Action<string> onSuccess, Action<string> onError)
    {
        string payload = "{" + JsonField("path", path) + "}";
        SendRequest("firestoreGet", payload, onSuccess, onError);
    }

    public void FirestoreGetCollection(string path, Action<string> onSuccess, Action<string> onError)
    {
        string payload = "{" + JsonField("path", path) + "}";
        SendRequest("firestoreGetCollection", payload, onSuccess, onError);
    }

    public void FirestoreGetCollectionOrdered(
        string path,
        string orderByField,
        bool descending,
        int limit,
        Action<string> onSuccess,
        Action<string> onError)
    {
        int safeLimit = Mathf.Clamp(limit, 1, 100);
        string payload = "{" +
            JsonField("path", path) + "," +
            JsonField("orderByField", orderByField) + "," +
            "\"descending\":" + (descending ? "true" : "false") + "," +
            "\"limit\":" + safeLimit +
        "}";

        SendRequest("firestoreGetCollectionOrdered", payload, onSuccess, onError);
    }

    public void CompleteQuest(int questNumber, string dataJson, Action<bool, string> callback)
    {
        string payload = "{" +
            "\"questNumber\":" + questNumber + "," +
            JsonField("dataJson", dataJson ?? "") +
        "}";

        SendRequest("completeQuest", payload,
            dataJsonResponse =>
            {
                UpsertCachedQuest(questNumber, true, dataJson ?? "");
                callback?.Invoke(true, string.IsNullOrEmpty(dataJsonResponse) ? "Quest completed" : dataJsonResponse);
            },
            error => callback?.Invoke(false, error));
    }

    public void GetQuestProgress(Action<QuestProgressData[]> onSuccess, Action<string> onError)
    {
        if (hasPortalInit)
        {
            onSuccess?.Invoke(cachedQuestProgress ?? new QuestProgressData[0]);
            return;
        }

        SendRequest("getQuestProgress", "{}", dataJson =>
        {
            QuestProgressEnvelope envelope = JsonUtility.FromJson<QuestProgressEnvelope>(dataJson);
            cachedQuestProgress = envelope != null && envelope.quests != null
                ? envelope.quests
                : new QuestProgressData[0];
            onSuccess?.Invoke(cachedQuestProgress);
        }, onError);
    }

    public void UpdateAvatar(string userId, int avatarIndex, Action<bool> onComplete)
    {
        string payload = "{" +
            JsonField("userId", userId ?? currentUserId) + "," +
            "\"avatarIndex\":" + avatarIndex +
        "}";

        SendRequest("updateAvatar", payload,
            _ =>
            {
                PlayerPrefs.SetInt(PlayerPrefsAvatarIndex, avatarIndex);
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    PlayerPrefs.SetInt(PlayerPrefsAvatarSelectedPrefix + currentUserId, 1);
                }
                PlayerPrefs.Save();
                onComplete?.Invoke(true);
            },
            _ => onComplete?.Invoke(false));
    }

    public void GetUserData(string userId, Action<UserData> onSuccess, Action<string> onError)
    {
        if (cachedSession != null && string.Equals(userId, cachedSession.userId, StringComparison.Ordinal))
        {
            onSuccess?.Invoke(new UserData
            {
                id = cachedSession.userId,
                username = cachedSession.username,
                email = cachedSession.email,
                avatar_index = cachedSession.avatar_index,
                avatar_selected = cachedSession.avatar_selected
            });
            return;
        }

        string payload = "{" + JsonField("userId", userId) + "}";
        SendRequest("getUserData", payload,
            dataJson => onSuccess?.Invoke(JsonUtility.FromJson<UserData>(dataJson)),
            onError);
    }

    public void CreateBrick(string userId, string name, string company, string message, Action<bool, string> callback)
    {
        string payload = "{" +
            JsonField("userId", userId ?? currentUserId) + "," +
            JsonField("name", name) + "," +
            JsonField("company", company) + "," +
            JsonField("message", message) +
        "}";

        SendRequest("createBrick", payload,
            dataJson => callback?.Invoke(true, dataJson),
            error => callback?.Invoke(false, error));
    }

    public void CreatePersonalProfile(UserData data, Action<bool, string> callback)
    {
        string payload = JsonUtility.ToJson(data ?? new UserData());
        SendRequest("createPersonalProfile", payload,
            dataJson => callback?.Invoke(true, dataJson),
            error => callback?.Invoke(false, error));
    }

    public void OnPortalMessage(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        PortalMessage message;
        try
        {
            message = JsonUtility.FromJson<PortalMessage>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PortalBridgeBackend] Failed to parse portal message: {exception.Message}");
            return;
        }

        if (message == null || message.source != SourcePortal)
        {
            return;
        }

        if (message.type == MessageTypePortalInit)
        {
            HandlePortalInit(json);
            return;
        }

        if (message.type == MessageTypeResponse)
        {
            HandlePortalResponse(message);
        }
    }

    private void HandlePortalInit(string json)
    {
        PortalInitMessage init = JsonUtility.FromJson<PortalInitMessage>(json);
        if (init == null || init.payload == null || init.payload.user == null)
        {
            Debug.LogWarning("[PortalBridgeBackend] Portal init did not include a user payload.");
            pendingAutoLoginError?.Invoke("Portal init did not include user data.");
            return;
        }

        BackendResponse response = new BackendResponse
        {
            userId = init.payload.user.id,
            username = FirstNonEmpty(init.payload.user.username, init.payload.user.fullName, "User"),
            email = init.payload.user.email,
            avatar_index = init.payload.gameProfile != null ? init.payload.gameProfile.avatarIndex : 0,
            avatar_selected = init.payload.gameProfile != null && init.payload.gameProfile.avatarSelected,
            message = "Portal session initialized"
        };

        cachedQuestProgress = init.payload.quests ?? new QuestProgressData[0];
        hasPortalInit = true;
        ApplySession(response);

        pendingAutoLoginSuccess?.Invoke(response);
        pendingAutoLoginSuccess = null;
        pendingAutoLoginError = null;

        Debug.Log($"[PortalBridgeBackend] Portal session initialized for {response.userId}");
    }

    private void HandlePortalResponse(PortalMessage message)
    {
        if (string.IsNullOrEmpty(message.requestId) || !pendingRequests.TryGetValue(message.requestId, out PendingRequest request))
        {
            return;
        }

        pendingRequests.Remove(message.requestId);

        if (!message.ok)
        {
            request.onError?.Invoke(string.IsNullOrEmpty(message.error) ? "Portal request failed." : message.error);
            return;
        }

        string dataJson = !string.IsNullOrEmpty(message.dataJson) ? message.dataJson : "{}";
        request.onSuccess?.Invoke(dataJson);
    }

    private void SendRequest(string op, string payloadJson, Action<string> onSuccess, Action<string> onError)
    {
        string requestId = CreateRequestId(op);
        pendingRequests[requestId] = new PendingRequest
        {
            onSuccess = onSuccess,
            onError = onError
        };

        string requestJson = "{" +
            JsonField("source", SourceUnity) + "," +
            JsonField("type", MessageTypeRequest) + "," +
            JsonField("requestId", requestId) + "," +
            JsonField("op", op) + "," +
            "\"payload\":" + NormalizePayload(payloadJson) +
        "}";

#if !UNITY_EDITOR && UNITY_WEBGL
        Portal_Request(requestJson);
#else
        Debug.Log($"[PortalBridgeBackend] Editor request: {requestJson}");
        if (op == "getSession")
        {
            string userId = PlayerPrefs.GetString(PlayerPrefsUserId, "");
            if (string.IsNullOrEmpty(userId))
            {
                pendingRequests.Remove(requestId);
                onError?.Invoke("No portal session is available in the Unity editor.");
                return;
            }

            pendingRequests.Remove(requestId);
            onSuccess?.Invoke(JsonUtility.ToJson(new BackendResponse
            {
                userId = userId,
                username = PlayerPrefs.GetString(PlayerPrefsUsername, "Editor User"),
                email = PlayerPrefs.GetString(PlayerPrefsEmail, ""),
                avatar_index = PlayerPrefs.GetInt(PlayerPrefsAvatarIndex, 0),
                avatar_selected = true,
                message = "Editor PlayerPrefs session"
            }));
            return;
        }

        pendingRequests.Remove(requestId);
        onSuccess?.Invoke("{}");
#endif
    }

    private void ApplySession(BackendResponse response)
    {
        cachedSession = response;
        currentUserId = response.userId ?? "";

        if (!string.IsNullOrEmpty(currentUserId))
        {
            PlayerPrefs.SetString(PlayerPrefsUserId, currentUserId);
            PlayerPrefs.SetInt(PlayerPrefsRememberMe, 1);
            PlayerPrefs.SetInt(PlayerPrefsAvatarSelectedPrefix + currentUserId, response.avatar_selected ? 1 : 0);
        }

        PlayerPrefs.SetString(PlayerPrefsUsername, response.username ?? "");
        PlayerPrefs.SetString(PlayerPrefsEmail, response.email ?? "");
        PlayerPrefs.SetInt(PlayerPrefsAvatarIndex, response.avatar_index);
        PlayerPrefs.Save();
    }

    private BackendResponse ParseBackendResponse(string dataJson)
    {
        if (string.IsNullOrEmpty(dataJson) || dataJson == "{}")
        {
            return null;
        }

        return JsonUtility.FromJson<BackendResponse>(dataJson);
    }

    private void UpsertCachedQuest(int questNumber, bool completed, string dataJson)
    {
        List<QuestProgressData> quests = new List<QuestProgressData>(cachedQuestProgress ?? new QuestProgressData[0]);
        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].questNumber == questNumber)
            {
                quests[i].completed = completed;
                quests[i].dataJson = dataJson;
                cachedQuestProgress = quests.ToArray();
                return;
            }
        }

        quests.Add(new QuestProgressData
        {
            questNumber = questNumber,
            completed = completed,
            dataJson = dataJson
        });
        cachedQuestProgress = quests.ToArray();
    }

    private static string CreateRequestId(string op)
    {
        return $"{op}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{UnityEngine.Random.Range(1000, 9999)}";
    }

    private static string NormalizePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return "{}";
        }

        string trimmed = payloadJson.Trim();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal)
            ? trimmed
            : "{}";
    }

    private static string JsonField(string key, string value)
    {
        return "\"" + EscapeJson(key) + "\":\"" + EscapeJson(value ?? "") + "\"";
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (string value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }

    [Serializable]
    private class PortalMessage
    {
        public string source;
        public string type;
        public string requestId;
        public bool ok;
        public string error;
        public string dataJson;
    }

    [Serializable]
    private class PortalInitMessage
    {
        public string source;
        public string type;
        public PortalInitPayload payload;
    }

    [Serializable]
    private class PortalInitPayload
    {
        public PortalUser user;
        public PortalGameProfile gameProfile;
        public QuestProgressData[] quests;
    }

    [Serializable]
    private class PortalUser
    {
        public string id;
        public string email;
        public string username;
        public string fullName;
        public string membershipTier;
        public string role;
    }

    [Serializable]
    private class PortalGameProfile
    {
        public int avatarIndex;
        public bool avatarSelected;
        public string currentScene;
    }

    [Serializable]
    private class QuestProgressEnvelope
    {
        public QuestProgressData[] quests;
    }
}
