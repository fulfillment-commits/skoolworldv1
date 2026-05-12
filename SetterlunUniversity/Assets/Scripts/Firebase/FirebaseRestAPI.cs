using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class FirebaseRestAPI : MonoBehaviour
{
    public static FirebaseRestAPI Instance;

    [Header("Firebase Config")]
    public string projectId = "setterlun-university";
    public string apiKey = "AIzaSyCDAwzCloZf00baxR7nIREY9Mx1aE73krU";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persists across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveUser(string fullName, string username, string email, string phone, string timezone, string discovery)
    {
        StartCoroutine(SendUserData(fullName, username, email, phone, timezone, discovery));
    }

    IEnumerator SendUserData(string fullName, string username, string email, string phone, string timezone, string discovery)
    {
        string url = $"https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents/users?key={apiKey}";

        string json = @"
        {
          ""fields"": {
            ""fullName"": { ""stringValue"": """ + fullName + @""" },
            ""username"": { ""stringValue"": """ + username + @""" },
            ""email"": { ""stringValue"": """ + email + @""" },
            ""phone"": { ""stringValue"": """ + phone + @""" },
            ""timezone"": { ""stringValue"": """ + timezone + @""" },
            ""discoverySource"": { ""stringValue"": """ + discovery + @""" }
          }
        }";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("User Saved Successfully!");
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            Debug.LogError(request.downloadHandler.text);
        }
    }
}