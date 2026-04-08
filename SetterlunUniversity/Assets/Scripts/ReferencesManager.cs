using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityStandardAssets.CrossPlatformInput;

public class ReferencesManager : MonoBehaviour
{
    public static ReferencesManager Instance;
    public GameObject player;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
