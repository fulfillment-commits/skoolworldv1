using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class VideoPlayerManager : MonoBehaviour
{
    public static VideoPlayerManager Instance;

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Video Files (names inside StreamingAssets)")]
    public List<string> videoFiles = new List<string>(); // e.g., "video1.mp4"

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayVideo(int index)
    {
        if (index < 0 || index >= videoFiles.Count)
        {
            Debug.LogWarning("Invalid video index");
            return;
        }

        PlayVideoURL(videoFiles[index]);
    }

    public void PauseVideo()
    {
        videoPlayer.Pause();
    }

    public void ResumeVideo()
    {
        videoPlayer.Pause(); // Toggle pause to resume
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
    }

    public void PlayVideo()
    {
        videoPlayer.Play();
    }

    public void RestartVideo()
    {
        videoPlayer.Stop();
        PlayVideo();
    }

    private void PlayVideoURL(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            Debug.LogWarning("Video file name is empty");
            return;
        }

        string url = "";

#if UNITY_WEBGL && !UNITY_EDITOR
        url = Application.streamingAssetsPath + "/" + fileName; // WebGL requires HTTP path
#else
        url = "file://" + Application.streamingAssetsPath + "/" + fileName; // Editor/PC
#endif

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.Play();

        Debug.Log("Playing video: " + url);
    }
}
