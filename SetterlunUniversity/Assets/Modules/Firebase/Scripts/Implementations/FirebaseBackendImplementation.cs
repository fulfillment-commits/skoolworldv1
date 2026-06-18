using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class FirebaseBackendImplementation : MonoBehaviour, IBackendService
{
    [DllImport("__Internal")]
    private static extern void Firebase_Login(string email, string password);
    [DllImport("__Internal")]
    private static extern void Firebase_Register(string email, string password, string username);
    [DllImport("__Internal")]
    private static extern void Firebase_Initialize(string configJson, string callbackObjName);
    [DllImport("__Internal")]
    private static extern void Firebase_SetRememberMe(int rememberMe);
    [DllImport("__Internal")]
    private static extern void Firebase_TryAutoLogin();
    [DllImport("__Internal")]
    private static extern void Firebase_Logout();
    [DllImport("__Internal")]
    private static extern void Firebase_Firestore_Set(string path, string json);
    [DllImport("__Internal")]
    private static extern void Firebase_Firestore_Update(string path, string json);
    [DllImport("__Internal")]
    private static extern void Firebase_Firestore_Get(string path);
    [DllImport("__Internal")]
    private static extern void Firebase_Firestore_GetCollection(string path);
    [DllImport("__Internal")]
    private static extern void Firebase_Firestore_GetCollectionOrdered(string path, string orderByField, int descending, int limit);

    private Action<BackendResponse> loginSuccess;
    private Action<BackendResponse> registerSuccess;
    private Action<BackendResponse> autoLoginSuccess;
    private Action<string> currentError;
    
    // Support for multiple concurrent Firestore requests
    private Dictionary<string, Action<bool, string>> pendingGenericCallbacks = new Dictionary<string, Action<bool, string>>();
    private Dictionary<string, Action<string>> pendingDataCallbacks = new Dictionary<string, Action<string>>();
    private Dictionary<string, Action<string>> pendingCollectionCallbacks = new Dictionary<string, Action<string>>();
    private Dictionary<string, Action<QuestProgressData[]>> pendingQuestCallbacks = new Dictionary<string, Action<QuestProgressData[]>>();

    private string currentUserId = "";
    private string configJson = "";

    public void Setup(string json)
    {
        configJson = json;
        #if !UNITY_EDITOR && UNITY_WEBGL
        Debug.Log($"🔥 [FirebaseBackend] Setup called, initializing for {gameObject.name}...");
        Firebase_Initialize(configJson, gameObject.name);
        #endif
    }

    public void SetUserId(string userId)
    {
        currentUserId = userId;
        Debug.Log($"🔥 [FirebaseBackend] User ID synced: {userId}");
    }

    public void SetRememberMe(bool rememberMe)
    {
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_SetRememberMe(rememberMe ? 1 : 0);
        #endif
    }

    public void TryAutoLogin(Action<BackendResponse> onSuccess, Action<string> onError)
    {
        autoLoginSuccess = (res) => {
            currentUserId = res.userId;
            onSuccess?.Invoke(res);
        };
        currentError = onError;

        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_TryAutoLogin();
        #else
        string userId = PlayerPrefs.GetString("OnboardingUserId_Str", "");
        if (!string.IsNullOrEmpty(userId))
        {
            onSuccess?.Invoke(new BackendResponse {
                userId = userId,
                username = PlayerPrefs.GetString("OnboardingUsername", ""),
                email = PlayerPrefs.GetString("OnboardingEmail", ""),
                avatar_index = PlayerPrefs.GetInt("OnboardingAvatarIndex", 0),
                message = "Editor auto-login from PlayerPrefs"
            });
        }
        else
        {
            onError?.Invoke("No saved local session.");
        }
        #endif
    }

    public void FirestoreSet(string path, string json, Action<bool, string> callback)
    {
        pendingGenericCallbacks[path] = callback;
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Firestore_Set(path, json);
        #else
        callback?.Invoke(true, "Editor Stub");
        #endif
    }

    public void FirestoreUpdate(string path, string json, Action<bool, string> callback)
    {
        pendingGenericCallbacks[path] = callback;
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Firestore_Update(path, json);
        #else
        callback?.Invoke(true, "Editor Stub");
        #endif
    }

    public void FirestoreGet(string path, Action<string> onSuccess, Action<string> onError)
    {
        pendingDataCallbacks[path] = onSuccess;
        currentError = onError;
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Firestore_Get(path);
        #else
        onSuccess?.Invoke("{}");
        #endif
    }

    public void FirestoreGetCollection(string path, Action<string> onSuccess, Action<string> onError)
    {
        string requestKey = path;
        pendingCollectionCallbacks[requestKey] = onSuccess;
        currentError = onError;

        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Firestore_GetCollection(path);
        #else
        onSuccess?.Invoke(CreateEmptyCollectionJson(path, requestKey));
        #endif
    }

    public void FirestoreGetCollectionOrdered(string path, string orderByField, bool descending, int limit, Action<string> onSuccess, Action<string> onError)
    {
        int safeLimit = Mathf.Clamp(limit, 1, 100);
        string requestKey = BuildCollectionRequestKey(path, orderByField, descending, safeLimit);
        pendingCollectionCallbacks[requestKey] = onSuccess;
        currentError = onError;

        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Firestore_GetCollectionOrdered(path, orderByField, descending ? 1 : 0, safeLimit);
        #else
        onSuccess?.Invoke(CreateEmptyCollectionJson(path, requestKey));
        #endif
    }

    public void Login(string login, string password, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        loginSuccess = (res) => {
            currentUserId = res.userId;
            onSuccess?.Invoke(res);
        };
        currentError = onError;
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Login(login, password);
        #else
        Debug.LogWarning("Firebase WebGL Bridge only works in a WebGL build!");
        onSuccess?.Invoke(new BackendResponse { userId = "1", username = login, message = "Editor Stub" });
        #endif
    }

    public void Register(UserData data, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        registerSuccess = (res) => {
            currentUserId = res.userId;
            onSuccess?.Invoke(res);
        };
        currentError = onError;
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Register(data.email, data.password, data.username);
        #else
        onSuccess?.Invoke(new BackendResponse { userId = "1", username = data.username });
        #endif
    }

    public void Logout()
    {
        currentUserId = "";
        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Logout();
        #endif
    }

    // ====================== Quests (Implemented via Generic Firestore) ======================
    public void CompleteQuest(int questNumber, string dataJson, Action<bool, string> callback)
    {
        string path = $"users/{currentUserId}/quests/{questNumber}";
        var data = new QuestSaveData { completed = true, data_json = dataJson, timestamp = DateTime.UtcNow.ToString() };
        FirestoreSet(path, JsonUtility.ToJson(data), callback);
    }

    [Serializable]
    private class QuestSaveData { public bool completed; public string data_json; public string timestamp; }

    public void GetQuestProgress(Action<QuestProgressData[]> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(currentUserId))
        {
            onSuccess?.Invoke(new QuestProgressData[0]);
            return;
        }

        string path = $"users/{currentUserId}/quests";
        pendingQuestCallbacks[path] = onSuccess;
        currentError = onError;

        #if !UNITY_EDITOR && UNITY_WEBGL
        Firebase_Firestore_GetCollection(path);
        #else
        onSuccess?.Invoke(new QuestProgressData[0]);
        #endif
    }

    public void OnFirebaseCollectionSuccess(string json)
    {
        try
        {
            var response = JsonUtility.FromJson<CollectionResponse>(json);
            if (response == null)
            {
                return;
            }

            string requestKey = string.IsNullOrEmpty(response.requestKey) ? response.path : response.requestKey;
            if (response != null && pendingCollectionCallbacks.TryGetValue(requestKey, out var collectionCallback))
            {
                collectionCallback?.Invoke(json);
                pendingCollectionCallbacks.Remove(requestKey);
                return;
            }

            if (pendingQuestCallbacks.TryGetValue(response.path, out var callback))
            {
                List<QuestProgressData> progress = new List<QuestProgressData>();
                if (response.items != null)
                {
                    foreach (var item in response.items)
                    {
                        // Parse the internal quest data from the stringified JSON
                        var questData = JsonUtility.FromJson<QuestSaveData>(item.data);

                        progress.Add(new QuestProgressData {
                            questNumber = int.Parse(item.id),
                            completed = questData.completed,
                            dataJson = questData.data_json
                        });
                    }
                }
                
                callback?.Invoke(progress.ToArray());
                pendingQuestCallbacks.Remove(response.path);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseBackend] Collection Parse Error: {e.Message}");
        }
    }

    [Serializable]
    private class CollectionResponse { public string path; public string requestKey; public CollectionItem[] items; }
    [Serializable]
    private class CollectionItem { public string id; public string data; }

    private static string BuildCollectionRequestKey(string path, string orderByField, bool descending, int limit)
    {
        return $"{path}|{orderByField}|{(descending ? "desc" : "asc")}|{limit}";
    }

    private static string CreateEmptyCollectionJson(string path, string requestKey)
    {
        return JsonUtility.ToJson(new CollectionResponse {
            path = path,
            requestKey = requestKey,
            items = new CollectionItem[0]
        });
    }

    // ====================== User Data (Implemented via Generic Firestore) ======================
    public void UpdateAvatar(string userId, int avatarIndex, Action<bool> onComplete)
    {
        string path = $"users/{userId}";
        var data = new AvatarUpdateData { avatar_index = avatarIndex };
        FirestoreUpdate(path, JsonUtility.ToJson(data), (success, msg) => onComplete?.Invoke(success));
    }

    [Serializable]
    private class AvatarUpdateData { public int avatar_index; }

    public void GetUserData(string userId, Action<UserData> onSuccess, Action<string> onError)
    {
        string path = $"users/{userId}";
        FirestoreGet(path, 
            onSuccess: (json) => onSuccess?.Invoke(JsonUtility.FromJson<UserData>(json)),
            onError: onError);
    }

    public void CreateBrick(string userId, string name, string company, string message, Action<bool, string> callback)
    {
        string path = $"bricks/{userId}";
        var data = new BrickSaveData { user_id = userId, name_on_brick = name, business_name = company, message = message };
        FirestoreSet(path, JsonUtility.ToJson(data), callback);
    }

    [Serializable]
    private class BrickSaveData { public string user_id; public string name_on_brick; public string business_name; public string message; }

    public void CreatePersonalProfile(UserData data, Action<bool, string> callback)
    {
        string path = $"users/{data.id}";
        FirestoreUpdate(path, JsonUtility.ToJson(data), callback);
    }

    // ====================== Callbacks from JS ======================
    public void OnFirebaseGenericSuccess(string path)
    {
        if (pendingGenericCallbacks.TryGetValue(path, out var callback))
        {
            callback?.Invoke(true, path);
            pendingGenericCallbacks.Remove(path);
        }
    }

    public void OnFirebaseGenericError(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<GenericErrorWrapper>(json);
            if (wrapper != null && pendingGenericCallbacks.TryGetValue(wrapper.path, out var callback))
            {
                callback?.Invoke(false, wrapper.message);
                pendingGenericCallbacks.Remove(wrapper.path);
                return;
            }

            currentError?.Invoke(wrapper != null ? wrapper.message : json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseBackend] Generic Error Parse Error: {e.Message}");
            currentError?.Invoke(json);
        }
    }

    public void OnFirebaseGenericDataSuccess(string json)
    {
        try {
            var wrapper = JsonUtility.FromJson<GenericDataWrapper>(json);
            if (wrapper != null && pendingDataCallbacks.TryGetValue(wrapper.path, out var callback))
            {
                if (!string.IsNullOrEmpty(wrapper.data) && wrapper.data != "null" && wrapper.data != "{}")
                {
                    callback?.Invoke(wrapper.data);
                }
                else
                {
                    callback?.Invoke("{}");
                }
                pendingDataCallbacks.Remove(wrapper.path);
            }
        } catch (Exception e) {
            Debug.LogError($"[FirebaseBackend] Generic Data Parse Error: {e.Message}");
        }
    }

    [Serializable]
    private class GenericDataWrapper { public string path; public string data; }

    [Serializable]
    private class GenericErrorWrapper { public string path; public string message; }

    public void OnFirebaseLoginSuccess(string json)
    {
        var data = JsonUtility.FromJson<BackendResponse>(json);
        loginSuccess?.Invoke(data);
    }

    public void OnFirebaseAutoLoginSuccess(string json)
    {
        var data = JsonUtility.FromJson<BackendResponse>(json);
        autoLoginSuccess?.Invoke(data);
    }

    public void OnFirebaseAutoLoginFailed(string message)
    {
        currentError?.Invoke(message);
    }

    public void OnFirebaseRegisterSuccess(string json)
    {
        var data = JsonUtility.FromJson<BackendResponse>(json);
        registerSuccess?.Invoke(data);
    }

    public void OnFirebaseError(string message)
    {
        currentError?.Invoke(message);
    }
}
