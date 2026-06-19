using System;

public interface IBackendService
{
    // Generic Firestore Operations
    void FirestoreSet(string path, string json, Action<bool, string> callback);
    void FirestoreUpdate(string path, string json, Action<bool, string> callback);
    void FirestoreGet(string path, Action<string> onSuccess, Action<string> onError);
    void FirestoreGetCollection(string path, Action<string> onSuccess, Action<string> onError);
    void FirestoreGetCollectionOrdered(string path, string orderByField, bool descending, int limit, Action<string> onSuccess, Action<string> onError);
    
    // Auth
    void SetUserId(string userId);
    void SetRememberMe(bool rememberMe);
    void TryAutoLogin(Action<BackendResponse> onSuccess, Action<string> onError);
    void Login(string login, string password, Action<BackendResponse> onSuccess, Action<string> onError);
    void Register(UserData data, Action<BackendResponse> onSuccess, Action<string> onError);
    void Logout();

    // Quests
    void CompleteQuest(int questNumber, string dataJson, Action<bool, string> callback);
    void GetQuestProgress(Action<QuestProgressData[]> onSuccess, Action<string> onError);

    // User Data
    void UpdateAvatar(string userId, int avatarIndex, Action<bool> onComplete);
    void GetUserData(string userId, Action<UserData> onSuccess, Action<string> onError);

    // Specific Quests/Actions
    void CreateBrick(string userId, string name, string company, string message, Action<bool, string> callback);
    void CreatePersonalProfile(UserData data, Action<bool, string> callback);
}

[Serializable]
public class UserData
{
    public string id;
    public string full_name;
    public string username;
    public string email;
    public string phone;
    public string password;
    public int avatar_index;
    public bool avatar_selected;
}

[Serializable]
public class BackendResponse
{
    public string userId;
    public string username;
    public string email;
    public int avatar_index;
    public bool avatar_selected;
    public string token;
    public string message;
}

[Serializable]
public class QuestProgressData
{
    public int questNumber;
    public bool completed;
    public string dataJson;
}

[Serializable]
public class FirestoreCollectionResponse
{
    public string path;
    public string requestKey;
    public FirestoreCollectionItem[] items;
}

[Serializable]
public class FirestoreCollectionItem
{
    public string id;
    public string data;
}
