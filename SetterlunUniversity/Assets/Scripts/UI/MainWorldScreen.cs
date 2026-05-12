using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainWorldScreen : ScreenBase
{
    [Header("Quest Panel")]
    [SerializeField] private QuestPanelUI questPanel;

    [Header("World UI Elements")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private Button settingsButton;

    protected override void OnShow()
    {
        base.OnShow();
        
        // Refresh the quest panel whenever the screen is shown
        if (questPanel != null)
        {
            questPanel.Refresh();
        }
        else if (QuestPanelUI.Instance != null)
        {
            QuestPanelUI.Instance.Refresh();
        }

        UpdateWelcomeMessage();
    }

    private void Start()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
    }

    private void UpdateWelcomeMessage()
    {
        if (welcomeText != null && OnboardingQuestManager.Instance != null)
        {
            string username = OnboardingQuestManager.Instance.CurrentUsername;
            welcomeText.text = string.IsNullOrEmpty(username) ? "Welcome to Setterlun!" : $"Welcome back, {username}!";
        }
    }

    private void OnSettingsClicked()
    {
        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.GoToSettings();
        }
    }
}
