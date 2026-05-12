using UnityEngine;
using UnityEngine.UI;

public class WelcomeScreen : ScreenBase
{
    [Header("Buttons")]
    [SerializeField] private Button startJourneyButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;

    private void Start()
    {
        if (startJourneyButton != null)
            startJourneyButton.onClick.AddListener(OnStartJourneyClicked);
        
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
    }

    private void OnStartJourneyClicked()
    {
        if (OnboardingManager.Instance != null)
        {
            OnboardingManager.Instance.StartJourney();
        }
        else
        {
            // Fallback if OnboardingManager is missing
            ScreenManager.Instance.ShowScreen(ScreenType.FastRegister);
        }
    }

    private void OnContinueClicked()
    {
        ScreenManager.Instance.GoToLogin();
    }

    private void OnSettingsClicked()
    {
        ScreenManager.Instance.GoToSettings();
    }
}
