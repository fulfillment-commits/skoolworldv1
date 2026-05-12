using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AvatarScreen : ScreenBase
{
    [Header("Avatar Configuration")]
    [SerializeField] private Sprite[] avatarSprites;
    [SerializeField] private Image selectedAvatarImage;
    [SerializeField] private TextMeshProUGUI selectedAvatarNameText;

    [Header("Navigation")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button backButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI feedbackText;

    private int selectedAvatarIndex = 0;
    private const string PLAYERPREFS_AVATAR_INDEX = "OnboardingAvatarIndex";

    protected override void OnShow()
    {
        base.OnShow();
        ClearFeedback();
        LoadSavedAvatarIndex();
        UpdateSelectedAvatarDisplay();
    }

    private void Start()
    {
        if (leftArrowButton != null)
            leftArrowButton.onClick.AddListener(OnLeftArrowClicked);
        
        if (rightArrowButton != null)
            rightArrowButton.onClick.AddListener(OnRightArrowClicked);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnLeftArrowClicked()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;
        
        selectedAvatarIndex--;
        if (selectedAvatarIndex < 0)
            selectedAvatarIndex = avatarSprites.Length - 1;
            
        UpdateSelectedAvatarDisplay();
    }

    private void OnRightArrowClicked()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;

        selectedAvatarIndex++;
        if (selectedAvatarIndex >= avatarSprites.Length)
            selectedAvatarIndex = 0;

        UpdateSelectedAvatarDisplay();
    }

    private void LoadSavedAvatarIndex()
    {
        selectedAvatarIndex = PlayerPrefs.GetInt(PLAYERPREFS_AVATAR_INDEX, 0);
        // Ensure index is within bounds if sprites array changed
        if (avatarSprites != null && avatarSprites.Length > 0)
        {
            selectedAvatarIndex = Mathf.Clamp(selectedAvatarIndex, 0, avatarSprites.Length - 1);
        }
    }

    private void UpdateSelectedAvatarDisplay()
    {
        if (avatarSprites == null || avatarSprites.Length == 0) return;

        if (selectedAvatarImage != null && avatarSprites[selectedAvatarIndex] != null)
        {
            selectedAvatarImage.sprite = avatarSprites[selectedAvatarIndex];
        }

        if (selectedAvatarNameText != null)
        {
            selectedAvatarNameText.text = $"Avatar {selectedAvatarIndex + 1}";
        }
    }

    private void OnContinueClicked()
    {
        // Save locally
        PlayerPrefs.SetInt(PLAYERPREFS_AVATAR_INDEX, selectedAvatarIndex);
        PlayerPrefs.Save();
        
        // Save to backend
        string userId = PlayerPrefs.GetString("OnboardingUserId_Str", "");
        if (!string.IsNullOrEmpty(userId))
        {
            BackendSettings.Instance.Service.UpdateAvatar(userId, selectedAvatarIndex, (success) => {
                if (success)
                {
                    OnboardingQuestManager.Instance?.SetAvatarIndex(selectedAvatarIndex);
                    OnboardingManager.Instance.EnterWorld();
                }
                else
                {
                    // If it fails, we still let them proceed but warn them.
                    ShowFeedback("Server sync failed. Your selection is saved locally.");
                    Debug.LogWarning("⚠️ Avatar sync failed, but proceeding with local save.");
                    
                    OnboardingQuestManager.Instance?.SetAvatarIndex(selectedAvatarIndex);
                    Invoke(nameof(ProceedAfterDelay), 1.5f);
                }
            });
        }
        else
        {
            Debug.LogWarning("⚠️ No User ID found for sync. Proceeding locally.");
            OnboardingQuestManager.Instance?.SetAvatarIndex(selectedAvatarIndex);
            OnboardingManager.Instance.EnterWorld();
        }
    }

    private void ProceedAfterDelay()
    {
        OnboardingManager.Instance.EnterWorld();
    }

    private void OnBackClicked()
    {
        ScreenManager.Instance.ShowScreen(ScreenType.Login);
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = Color.yellow;
        }
    }

    private void ClearFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = "";
        }
    }
}
