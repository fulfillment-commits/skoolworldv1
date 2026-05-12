using UnityEngine;
using System;
using System.Collections.Generic;

public class CustomAPIBackendImplementation : MonoBehaviour, IBackendService
{
    public void FirestoreSet(string path, string json, Action<bool, string> callback)
    {
        callback?.Invoke(true, "Custom API Stub");
    }

    public void FirestoreUpdate(string path, string json, Action<bool, string> callback)
    {
        callback?.Invoke(true, "Custom API Stub");
    }

    public void FirestoreGet(string path, Action<string> onSuccess, Action<string> onError)
    {
        onSuccess?.Invoke("{}");
    }

    public void SetUserId(string userId)
    {
        // Custom API uses UserAPI instance directly or handles it internally
    }

    public void Login(string login, string password, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        UserAPI.Instance.Login(login, password, (res) => {
            onSuccess?.Invoke(new BackendResponse {
                userId = (res.userId > 0 ? res.userId : (res.user != null ? res.user.id : 0)).ToString(),
                username = res.user?.username ?? login,
                email = res.user?.email,
                token = res.token,
                message = res.message
            });
        }, onError);
    }

    public void Register(UserData data, Action<BackendResponse> onSuccess, Action<string> onError)
    {
        var apiData = new UserAPI.UserData {
            full_name = data.full_name,
            username = data.username,
            email = data.email,
            phone = data.phone,
            password = data.password
        };

        UserAPI.Instance.CreateFullUser(apiData, (res) => {
            onSuccess?.Invoke(new BackendResponse {
                userId = res.userId.ToString(),
                username = data.username,
                email = data.email,
                message = res.message
            });
        }, onError);
    }

    public void CompleteQuest(int questNumber, string dataJson, Action<bool, string> callback)
    {
        OnboardingQuestManager.Instance.CompleteQuest(questNumber, dataJson);
        callback?.Invoke(true, "Quest completed");
    }

    public void GetQuestProgress(Action<QuestProgressData[]> onSuccess, Action<string> onError)
    {
        onSuccess?.Invoke(new QuestProgressData[0]);
    }

    public void UpdateAvatar(string userId, int avatarIndex, Action<bool> onComplete)
    {
        onComplete?.Invoke(true);
    }

    public void GetUserData(string userId, Action<UserData> onSuccess, Action<string> onError)
    {
        onSuccess?.Invoke(new UserData { id = userId });
    }

    public void CreateBrick(string userId, string name, string company, string message, Action<bool, string> callback)
    {
        // Custom API logic (using old API endpoints)
        var brickData = new { user_id = userId, name_on_brick = name, business_name = company, message = message };
        StartCoroutine(PostJson(ApiConfig.Bricks, brickData, callback));
    }

    public void CreatePersonalProfile(UserData data, Action<bool, string> callback)
    {
        StartCoroutine(PostJson(ApiConfig.PersonalProfiles, data, callback));
    }

    private System.Collections.IEnumerator PostJson<T>(string url, T data, Action<bool, string> callback)
    {
        string json = JsonUtility.ToJson(data);
        using (UnityEngine.Networking.UnityWebRequest request = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                callback?.Invoke(true, request.downloadHandler.text);
            else
                callback?.Invoke(false, request.error);
        }
    }
}
