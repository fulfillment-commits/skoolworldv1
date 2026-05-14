using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Place this on a trigger collider in the world.
/// When the player walks in, a prompt button appears.
/// Clicking it opens the assigned course panel via ScreenManager.
/// Works for both Business Model and Business Portal.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CourseTrigger : MonoBehaviour
{
    [Header("Which panel to open")]
    public ScreenType targetScreen = ScreenType.Course_BusinessPortal;

    [Header("Prompt Button")]
    [Tooltip("The world-space or screen-space UI button shown when player is inside the trigger.")]
    public Button promptButton;
    [Tooltip("Optional label on the prompt button.")]
    public TMP_Text promptLabel;
    [Tooltip("Text shown on the button, e.g. 'Open Business Portal'")]
    public string promptText = "Open Course";

    [Header("Direction Gate (optional)")]
    [Tooltip("Leave empty to show button regardless of player direction.")]
    public Transform requiredDirection;
    [Range(0f, 1f)]
    public float directionThreshold = 0.7f;

    private bool _playerInside = false;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (promptButton != null)
        {
            promptButton.onClick.RemoveAllListeners();
            promptButton.onClick.AddListener(OpenCoursePanel);
            promptButton.gameObject.SetActive(false);
        }

        if (promptLabel != null && !string.IsNullOrEmpty(promptText))
            promptLabel.text = promptText;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = true;

        // If no direction gate, show immediately
        if (requiredDirection == null && promptButton != null)
            promptButton.gameObject.SetActive(true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player") || requiredDirection == null) return;

        float dot = Vector3.Dot(
            other.transform.forward.normalized,
            requiredDirection.forward.normalized);

        if (promptButton != null)
            promptButton.gameObject.SetActive(dot > directionThreshold);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInside = false;

        if (promptButton != null)
            promptButton.gameObject.SetActive(false);
    }

    private void OpenCoursePanel()
    {
        if (ScreenManager.Instance == null)
        {
            Debug.LogError("[CourseTrigger] ScreenManager.Instance is null.");
            return;
        }

        ScreenManager.Instance.ShowScreen(targetScreen);

        // Hide the prompt while the panel is open
        if (promptButton != null)
            promptButton.gameObject.SetActive(false);
    }

    // Call this from a close/back button on the panel if you want the prompt to reappear
    public void OnPanelClosed()
    {
        if (_playerInside && promptButton != null && requiredDirection == null)
            promptButton.gameObject.SetActive(true);
    }
}
