using ASAD_Multiplyer.Network;
using ASAD_Multiplyer.PlayerController;
using Bozo.ModularCharacters;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AvatarScreen : ScreenBase
{
    [Header("Avatar Configuration")]
    [SerializeField] private CharacterData avatarData;
    [SerializeField] private OutfitSystem outFitSystem;
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
        if (avatarData == null || avatarData.characterObjects.Length == 0) return;
        
        selectedAvatarIndex--;
        if (selectedAvatarIndex < 0)
            selectedAvatarIndex = avatarData.characterObjects.Length - 1;
            
        UpdateSelectedAvatarDisplay();
    }

    private void OnRightArrowClicked()
    {
        if (avatarData == null || avatarData.characterObjects.Length == 0) return;

        selectedAvatarIndex++;
        if (selectedAvatarIndex >= avatarData.characterObjects.Length)
            selectedAvatarIndex = 0;

        UpdateSelectedAvatarDisplay();
    }

    private void LoadSavedAvatarIndex()
    {
        selectedAvatarIndex = PlayerPrefs.GetInt(PLAYERPREFS_AVATAR_INDEX, 0);
        Debug.Log($"selectedAvatarIndex: {selectedAvatarIndex}");
        // Ensure index is within bounds if sprites array changed
        if (avatarData != null && avatarData.characterObjects.Length > 0)
        {
            selectedAvatarIndex = Mathf.Clamp(selectedAvatarIndex, 0, avatarData.characterObjects.Length - 1);
        }
    }

    private void UpdateSelectedAvatarDisplay()
    {
        if (avatarData == null || avatarData.characterObjects.Length == 0) return;

        if (outFitSystem != null && avatarData.characterObjects[selectedAvatarIndex] != null)
        {
            outFitSystem.characterData = avatarData.characterObjects[selectedAvatarIndex];
            outFitSystem.LoadFromObject();
        }

        if (selectedAvatarNameText != null)
        {
            selectedAvatarNameText.text = $"{avatarData.characterObjects[selectedAvatarIndex].name}";
        }
    }

    private void OnContinueClicked()
    {
        // Save locally
        OnboardingManager.Instance?.MarkAvatarSelected(selectedAvatarIndex);
        Debug.Log($"selectedAvatarIndex: {PlayerPrefs.GetInt(PLAYERPREFS_AVATAR_INDEX, 0)}");
        
        // Save to backend
        string userId = PlayerPrefs.GetString("OnboardingUserId_Str", "");
        if (!string.IsNullOrEmpty(userId))
        {
            BackendSettings.Instance.Service.UpdateAvatar(userId, selectedAvatarIndex, (success) => {
                if (success)
                {
                    OnboardingQuestManager.Instance?.SetAvatarIndex(selectedAvatarIndex);
                    OnboardingManager.Instance.EnterWorldWhenNetworkPlayerReady();
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
            OnboardingManager.Instance.EnterWorldWhenNetworkPlayerReady();
        }
    }

    private void ProceedAfterDelay()
    {
        OnboardingManager.Instance.EnterWorldWhenNetworkPlayerReady();
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
