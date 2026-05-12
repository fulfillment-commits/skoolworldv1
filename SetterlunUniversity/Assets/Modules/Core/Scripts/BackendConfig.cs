using UnityEngine;

[CreateAssetMenu(fileName = "BackendConfig", menuName = "Setterlun/Backend Config")]
public class BackendConfig : ScriptableObject
{
    public BackendType activeBackend = BackendType.Firebase;

    [Header("Firebase Credentials")]
    public string apiKey;
    public string authDomain;
    public string projectId;
    public string storageBucket;
    public string messagingSenderId;
    public string appId;
    public string measurementId;

    public string GetFirebaseConfigJson()
    {
        return $"{{\"apiKey\":\"{apiKey}\",\"authDomain\":\"{authDomain}\",\"projectId\":\"{projectId}\",\"storageBucket\":\"{storageBucket}\",\"messagingSenderId\":\"{messagingSenderId}\",\"appId\":\"{appId}\",\"measurementId\":\"{measurementId}\"}}";
    }
}
