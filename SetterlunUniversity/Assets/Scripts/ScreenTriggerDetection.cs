using ASAD_Multiplyer.Network;
using UnityEngine;
using UnityEngine.UI;

public class ScreenTriggerDetection : MonoBehaviour
{
    private Button buttonToActivate;
    private Button buttonToDeActivate;
    public Transform requiredDirection; // Reference direction
    public float directionThreshold = 0.7f;
    public Transform cameraPos;
    // 1 = same direction, 0 = 90�, -1 = opposite

    public Renderer screenRenderer;


    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Initial check on entry

        buttonToActivate = GamePlayUIManager.Instance.EnterVideoButton;
        buttonToActivate.onClick.RemoveAllListeners();
        buttonToActivate.onClick.AddListener(ActivateVideo);

        buttonToDeActivate = GamePlayUIManager.Instance.ExitVideoButton;
        buttonToDeActivate.onClick.RemoveAllListeners();
        buttonToDeActivate.onClick.AddListener(DeActivateVideo);
    }

    void ActivateVideo()
    {
        // Placeholder for video activation logic
        VideoPlayerManager.Instance.videoPlayer.targetMaterialRenderer = screenRenderer;
        VideoPlayerManager.Instance.PlayVideo(0); // Play first video as example
        Camera cam = Camera.main;
        //cam.GetComponent<SmoothFollow>().enabled = false; // Disable camera follow for cinematic effect
        cam.GetComponent<SmoothCameraMove>().MoveCamera(cameraPos); // Disable camera follow for cinematic effect

        Debug.Log("Video Activated!");
        GameObject player = PUN_NetworkManager.nm.myPlayer;
        player.SetActive(false);
        GamePlayUIManager.Instance.moveCamera.enabled = false;
        GamePlayUIManager.Instance.VideoControls.SetActive(true);
        buttonToActivate.gameObject.SetActive(false);
        buttonToDeActivate.gameObject.SetActive(true);
    }

    void DeActivateVideo()
    {
        VideoPlayerManager.Instance.StopVideo(); // Play first video as example
        Camera cam = Camera.main;
        // ReferencesManager.Instance.player.SetActive(true);
        GamePlayUIManager.Instance.moveCamera.enabled = true;
        GamePlayUIManager.Instance.VideoControls.SetActive(false);

        cam.GetComponent<SmoothCameraMove>().StopMovingCamera(); // Reset camera position
        //cam.GetComponent<SmoothFollow>().enabled = true; // Disable camera follow for cinematic effect
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
        Debug.Log("Video DeActivated!");
        buttonToDeActivate.gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3 playerForward = other.transform.forward.normalized;
        Vector3 requiredForward = requiredDirection.forward.normalized;

        float dot = Vector3.Dot(playerForward, requiredForward);

        // Activate button only if facing correct direction
        buttonToActivate.gameObject.SetActive(dot > directionThreshold);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        buttonToActivate.gameObject.SetActive(false);
    }
}
