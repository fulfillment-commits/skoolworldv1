using UnityEngine;
using System;
using System.Collections.Generic;

public class LocalDemoBackendImplementation : MonoBehaviour, IBackendService
{
    private const string PREFS_USER_LIST = "LocalDemo_Users";
    private const string PREFS_QUEST_PREFIX = "LocalDemo_Quest_";
    private const string PREFS_AVATAR_PREFIX = "LocalDemo_Avatar_";
    private const string PREFS_USER_DATA_PREFIX = "LocalDemo_UserData_";

    [Serializable]
    private class LocalUserList { public List<LocalUserData> users = new List<LocalUserData>(); }
    
    [Serializable]
    private class LocalUserData { public string email; public string password; public string userId; public string username; }

    public void Login(string login, string password, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        Debug.Log($"🏠 [LocalDemo] Attempting Login: {login}");
        var userList = LoadUsers();
        var user = userList.users.Find(u => (u.email == login || u.username == login) && u.password == password);

        if (user != null)
        {
            onSuccess?.Invoke(new BackendResponse {
                userId = user.userId,
                username = user.username,
                email = user.email,
                message = "Local Login Successful"
            });
        }
        else
        {
            onError?.Invoke("Invalid local credentials.");
        }
    }

    public void Register(UserData data, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        Debug.Log($"🏠 [LocalDemo] Registering User: {data.username}");
        var userList = LoadUsers();
        
        if (userList.users.Exists(u => u.email == data.email || u.username == data.username))
        {
            onError?.Invoke("User already exists locally.");
            return;
        }

        string newId = UnityEngine.Random.Range(1000, 9999).ToString();
        var newUser = new LocalUserData { 
            email = data.email, 
            password = data.password, 
            userId = newId, 
            username = data.username 
        };
        
        userList.users.Add(newUser);
        SaveUsers(userList);

        // Save full profile data locally
        PlayerPrefs.SetString(PREFS_USER_DATA_PREFIX + newId, JsonUtility.ToJson(data));
        PlayerPrefs.Save();

        onSuccess?.Invoke(new BackendResponse {
            userId = newId,
            username = data.username,
            email = data.email,
            message = "Local Registration Successful"
        });
    }

    public void CompleteQuest(int questNumber, string dataJson, Action<bool, string> callback)
    {
        string currentUserId = PlayerPrefs.GetString("OnboardingUserId_Str", "0");
        Debug.Log($"🏠 [LocalDemo] Saving Quest {questNumber} for User {currentUserId}");
        
        PlayerPrefs.SetInt(PREFS_QUEST_PREFIX + currentUserId + "_" + questNumber, 1);
        if (!string.IsNullOrEmpty(dataJson))
        {
            PlayerPrefs.SetString(PREFS_QUEST_PREFIX + currentUserId + "_" + questNumber + "_Data", dataJson);
        }
        PlayerPrefs.Save();
        
        callback?.Invoke(true, "Quest saved locally");
    }

    public void UpdateAvatar(string userId, int avatarIndex, Action<bool> onComplete)
    {
        Debug.Log($"🏠 [LocalDemo] Updating Avatar to {avatarIndex} for User {userId}");
        PlayerPrefs.SetInt(PREFS_AVATAR_PREFIX + userId, avatarIndex);
        PlayerPrefs.Save();
        onComplete?.Invoke(true);
    }

    public void GetUserData(string userId, Action<UserData> onSuccess, Action<string> onError)
    {
        string json = PlayerPrefs.GetString(PREFS_USER_DATA_PREFIX + userId, "");
        if (!string.IsNullOrEmpty(json))
        {
            var data = JsonUtility.FromJson<UserData>(json);
            int avatar = PlayerPrefs.GetInt(PREFS_AVATAR_PREFIX + userId, 0);
            data.avatar_index = avatar;
            onSuccess?.Invoke(data);
        }
        else
        {
            onError?.Invoke("Local user data not found.");
        }
    }

    public void GetQuestProgress(Action<QuestProgressData[]> onSuccess, Action<string> onError)
    {
        string currentUserId = PlayerPrefs.GetString("OnboardingUserId_Str", "0");
        List<QuestProgressData> progress = new List<QuestProgressData>();

        for (int i = 1; i <= 10; i++)
        {
            if (PlayerPrefs.HasKey(PREFS_QUEST_PREFIX + currentUserId + "_" + i))
            {
                progress.Add(new QuestProgressData {
                    questNumber = i,
                    completed = true,
                    dataJson = PlayerPrefs.GetString(PREFS_QUEST_PREFIX + currentUserId + "_" + i + "_Data", "")
                });
            }
        }
        onSuccess?.Invoke(progress.ToArray());
    }

    // Helpers
    private LocalUserList LoadUsers()
    {
        string json = PlayerPrefs.GetString(PREFS_USER_LIST, "{\"users\":[]}");
        return JsonUtility.FromJson<LocalUserList>(json);
    }

    private void SaveUsers(LocalUserList list)
    {
        PlayerPrefs.SetString(PREFS_USER_LIST, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    public void SetUserId(string userId) { } // Not needed for local

    // ====================== Generic Firestore (Simulated with PlayerPrefs) ======================
    public void FirestoreSet(string path, string json, Action<bool, string> callback)
    {
        Debug.Log($"🏠 [LocalDemo] Firestore Set: {path}");
        PlayerPrefs.SetString("LocalFirestore_" + path, json);
        PlayerPrefs.Save();
        callback?.Invoke(true, path);
    }

    public void FirestoreUpdate(string path, string json, Action<bool, string> callback)
    {
        Debug.Log($"🏠 [LocalDemo] Firestore Update: {path}");
        // In local demo, update is same as set for simplicity
        PlayerPrefs.SetString("LocalFirestore_" + path, json);
        PlayerPrefs.Save();
        callback?.Invoke(true, path);
    }

    public void FirestoreGet(string path, Action<string> onSuccess, Action<string> onError)
    {
        Debug.Log($"🏠 [LocalDemo] Firestore Get: {path}");
        string data = PlayerPrefs.GetString("LocalFirestore_" + path, "{}");
        onSuccess?.Invoke(data);
    }

    public void CreateBrick(string userId, string name, string company, string message, Action<bool, string> callback)
    {
        var data = new { user_id = userId, name_on_brick = name, business_name = company, message = message };
        FirestoreSet($"bricks/{userId}", JsonUtility.ToJson(data), callback);
    }

    public void CreatePersonalProfile(UserData data, Action<bool, string> callback)
    {
        Debug.Log($"🏠 [LocalDemo] Updating Personal Profile for: {data.id}");
        
        // Update UserData in PlayerPrefs
        string existingJson = PlayerPrefs.GetString(PREFS_USER_DATA_PREFIX + data.id, "");
        if (!string.IsNullOrEmpty(existingJson))
        {
            var existingData = JsonUtility.FromJson<UserData>(existingJson);
            existingData.full_name = data.full_name;
            existingData.phone = data.phone;
            // Update other relevant fields
            PlayerPrefs.SetString(PREFS_USER_DATA_PREFIX + data.id, JsonUtility.ToJson(existingData));
        }
        else
        {
            PlayerPrefs.SetString(PREFS_USER_DATA_PREFIX + data.id, JsonUtility.ToJson(data));
        }

        // Also update username in user list
        var userList = LoadUsers();
        var user = userList.users.Find(u => u.userId == data.id);
        if (user != null)
        {
            if (!string.IsNullOrEmpty(data.full_name)) user.username = data.full_name;
            SaveUsers(userList);
        }

        PlayerPrefs.Save();
        callback?.Invoke(true, "Local profile updated");
    }
}
