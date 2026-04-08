using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoControlButtons : MonoBehaviour
{
    public void PauseVideo()
    {
        VideoPlayerManager.Instance.PauseVideo();
    }

    public void ResumeVideo()
    {
        VideoPlayerManager.Instance.PlayVideo();
    }

    public void StopVideo()
    {
        VideoPlayerManager.Instance.StopVideo();
        VideoPlayerManager.Instance.PauseVideo();
    }

    public void RestartVideo()
    {
        VideoPlayerManager.Instance.RestartVideo();
    }
}
