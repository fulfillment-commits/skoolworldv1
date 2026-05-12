using UnityEngine;
using System;

public static class FirebaseManager
{
    private static IBackendService Backend => BackendSettings.Instance.Service;

    /// <summary>
    /// Saves data to a specific Firestore path. Path format: "collection/document"
    /// </summary>
    public static void SetData(string path, object data, Action<bool, string> callback = null)
    {
        string json = JsonUtility.ToJson(data);
        Debug.Log($"🔥 [FirebaseManager] SetData to {path}");
        Backend.FirestoreSet(path, json, (success, msg) => {
            if (!success) Debug.LogError($"❌ [FirebaseManager] SetData failed: {msg}");
            callback?.Invoke(success, msg);
        });
    }

    /// <summary>
    /// Updates specific fields in a Firestore document. Path format: "collection/document"
    /// </summary>
    public static void UpdateData(string path, object data, Action<bool, string> callback = null)
    {
        string json = JsonUtility.ToJson(data);
        Debug.Log($"🔥 [FirebaseManager] UpdateData to {path}");
        Backend.FirestoreUpdate(path, json, (success, msg) => {
            if (!success) Debug.LogError($"❌ [FirebaseManager] UpdateData failed: {msg}");
            callback?.Invoke(success, msg);
        });
    }

    /// <summary>
    /// Retrieves a document from Firestore. Path format: "collection/document"
    /// </summary>
    public static void GetData<T>(string path, Action<T> onSuccess, Action<string> onError = null)
    {
        Debug.Log($"🔥 [FirebaseManager] GetData from {path}");
        Backend.FirestoreGet(path, 
            onSuccess: (json) => {
                try {
                    T data = JsonUtility.FromJson<T>(json);
                    onSuccess?.Invoke(data);
                } catch (Exception e) {
                    Debug.LogError($"❌ [FirebaseManager] GetData Parse Error: {e.Message}");
                    onError?.Invoke(e.Message);
                }
            },
            onError: (error) => {
                Debug.LogError($"❌ [FirebaseManager] GetData failed: {error}");
                onError?.Invoke(error);
            }
        );
    }

    // Helper for user-specific data
    public static string GetUserPath(string userId, string subPath = "")
    {
        string path = $"users/{userId}";
        if (!string.IsNullOrEmpty(subPath)) path += $"/{subPath}";
        return path;
    }
}
