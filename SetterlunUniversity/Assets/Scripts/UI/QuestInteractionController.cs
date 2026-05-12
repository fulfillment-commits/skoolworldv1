using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class QuestInteractionController : MonoBehaviour
{
    public static QuestInteractionController Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private Button interactionButton;
    [SerializeField] private TextMeshProUGUI interactionText;
    [SerializeField] private TextMeshProUGUI buttonText;

    private int currentQuestId = -1;
    private ScreenType targetScreen;
    private Action currentClickCallback;

    // Static events allow scene triggers to talk to this global UI without references
    public static Action<int, ScreenType, string, string, Action> OnRequestShow;
    public static Action<int> OnRequestHide;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Ensure this panel starts hidden
        if (interactionPanel != null) interactionPanel.SetActive(false);
        
        if (interactionButton != null)
        {
            interactionButton.onClick.RemoveAllListeners();
            interactionButton.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnEnable()
    {
        OnRequestShow += Show;
        OnRequestHide += Hide;
    }

    private void OnDisable()
    {
        OnRequestShow -= Show;
        OnRequestHide -= Hide;
    }

    private void Show(int questId, ScreenType screen, string title, string btnText, Action onClickCallback)
    {
        currentQuestId = questId;
        targetScreen = screen;
        currentClickCallback = onClickCallback;
        
        if (interactionText != null) interactionText.text = title;
        if (buttonText != null) buttonText.text = btnText;
        
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
            // Force it to the front of the UI
            interactionPanel.transform.SetAsLastSibling();
        }
        
        Debug.Log($"[QuestInteractionUI] Showing button for Quest {questId}");
    }

    private void Hide(int questId)
    {
        // Only hide if the request comes from the quest currently being shown
        if (currentQuestId == questId)
        {
            if (interactionPanel != null) interactionPanel.SetActive(false);
            currentQuestId = -1;
        }
    }

    private void OnButtonClicked()
    {
        // Execute the custom callback if provided
        if (currentClickCallback != null)
        {
            currentClickCallback.Invoke();
        }
        else if (ScreenManager.Instance != null && targetScreen != ScreenType.None)
        {
            // Only auto-show the screen if NO custom callback is provided
            ScreenManager.Instance.ShowScreen(targetScreen);
        }
        
        // Hide the interaction button once the quest screen opens
        if (interactionPanel != null) interactionPanel.SetActive(false);
    }
}
