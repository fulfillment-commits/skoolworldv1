using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OnboardingCompleteScreen : ScreenBase
{
    [Header("Steps/Sections")]
    [SerializeField] private GameObject[] sections;
    private int currentSection = 0;

    [Header("UI Elements")]
    [SerializeField] private Button nextSectionButton;
    [SerializeField] private Button backSectionButton;
    [SerializeField] private Button startFirstQuestButton;

    [Header("Section 1: Summary")]
    [SerializeField] private TextMeshProUGUI userLevelText;
    [SerializeField] private TextMeshProUGUI userStageText;
    [SerializeField] private TextMeshProUGUI keyWeaknessText;

    [Header("Section 2: 90-Day Path")]
    [SerializeField] private GameObject floorUnlockVisual;
    [SerializeField] private CourseCardUI[] courseCards;

    [Header("Section 3: Next Action")]
    [SerializeField] private TextMeshProUGUI finalCallToActionText;

    [System.Serializable]
    public class CourseCardUI
    {
        public string courseName;
        public GameObject unlockedVisual;
        public GameObject lockedVisual;
        public bool isUnlocked;

        public void Refresh()
        {
            if (unlockedVisual != null) unlockedVisual.SetActive(isUnlocked);
            if (lockedVisual != null) lockedVisual.SetActive(!isUnlocked);
        }
    }

    private void Start()
    {
        if (nextSectionButton != null)
            nextSectionButton.onClick.AddListener(OnNextSectionClicked);
            
        if (backSectionButton != null)
            backSectionButton.onClick.AddListener(OnBackSectionClicked);

        if (startFirstQuestButton != null)
            startFirstQuestButton.onClick.AddListener(OnStartFirstQuestClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();
        currentSection = 0;
        
        if (floorUnlockVisual != null) floorUnlockVisual.SetActive(true);
        
        UpdateSummary();
        UpdatePathVisuals();
        UpdateSectionUI();
    }

    private void UpdateSummary()
    {
        if (OnboardingManager.Instance == null) return;

        if (userLevelText != null) userLevelText.text = "Level: Onboarded";
        if (userStageText != null) userStageText.text = $"Stage: {OnboardingManager.Instance.GetUserStage()}";
        if (keyWeaknessText != null) keyWeaknessText.text = $"Key Weakness: {OnboardingManager.Instance.GetKeyWeakness()}";
    }

    private void UpdatePathVisuals()
    {
        // Example logic for unlocking courses based on stage
        string stage = OnboardingManager.Instance.GetUserStage();
        
        for (int i = 0; i < courseCards.Length; i++)
        {
            // First 2 courses always unlocked, others based on stage
            if (i < 2) courseCards[i].isUnlocked = true;
            else if (stage != "Early Builder" && i < 4) courseCards[i].isUnlocked = true;
            else if (stage == "Scaling Expert" || stage == "Market Leader") courseCards[i].isUnlocked = true;
            else courseCards[i].isUnlocked = false;

            courseCards[i].Refresh();
        }
    }

    private void UpdateSectionUI()
    {
        if (sections == null || sections.Length == 0) return;

        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i] != null) sections[i].SetActive(i == currentSection);
        }

        // Show/Hide navigation buttons
        if (nextSectionButton != null)
            nextSectionButton.gameObject.SetActive(currentSection < sections.Length - 1);
            
        if (backSectionButton != null)
            backSectionButton.gameObject.SetActive(currentSection > 0);

        if (startFirstQuestButton != null)
            startFirstQuestButton.gameObject.SetActive(currentSection == sections.Length - 1);
    }

    private void OnNextSectionClicked()
    {
        if (currentSection < sections.Length - 1)
        {
            currentSection++;
            UpdateSectionUI();
        }
    }

    private void OnBackSectionClicked()
    {
        if (currentSection > 0)
        {
            currentSection--;
            UpdateSectionUI();
        }
    }

    private void OnStartFirstQuestClicked()
    {
        Debug.Log("🚀 Starting First Real Quest!");
        // Deactivate final screen and go to main world
        Hide();
        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ShowScreen(ScreenType.MainWorld);
        }
        
        // You could trigger a new sequence here
    }
}
