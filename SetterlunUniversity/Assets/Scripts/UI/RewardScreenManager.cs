using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RewardScreenManager : ScreenBase
{
    public static RewardScreenManager Instance { get; private set; }

    [Header("UI References - Assign in Inspector")]
    public Image avatarImage;
    public TMP_Text levelText;
    public TMP_Text stageText;
    public TMP_Text focusAreaText;
    public Transform questCardsParent;
    public GameObject questCardPrefab;
    public Button continueButton;

    [Header("Example Data")]
    [SerializeField] private int playerLevel = 3;
    [SerializeField] private string currentStage = "Early Builder";
    [SerializeField] private string keyFocusArea = "Lead Generation & Offer Clarity";

    [System.Serializable]
    public class RecommendedQuest
    {
        public string title;
        public string description;
        public bool isUnlocked;
        public bool isRecommended;
    }

    [SerializeField] private List<RecommendedQuest> recommendedQuests = new List<RecommendedQuest>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Fill test data if empty
        if (recommendedQuests.Count == 0)
        {
            recommendedQuests.Add(new RecommendedQuest
            {
                title = "Offer Clarity & Positioning",
                description = "Define your irresistible offer in 7 days",
                isUnlocked = true,
                isRecommended = true
            });

            recommendedQuests.Add(new RecommendedQuest
            {
                title = "Lead Generation Mastery",
                description = "Build consistent leads without burning out",
                isUnlocked = true,
                isRecommended = false
            });

            recommendedQuests.Add(new RecommendedQuest
            {
                title = "High-Ticket Closing System",
                description = "Master sales calls and close with confidence",
                isUnlocked = false,
                isRecommended = false
            });
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
    }

    public void OnOnboardingComplete()
    {
        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.ShowScreen(ScreenType.OnboardingComplete);
        }
        else
        {
            Show();
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        RefreshUI();
    }

    public void RefreshUI()
    {
        // Calculate dynamic data based on completed quests
        UpdateDynamicData();

        // Populate Summary Section
        if (levelText != null)
            levelText.text = $"Level {playerLevel} • {currentStage}";

        if (stageText != null)
            stageText.text = $"Your Stage: {currentStage}";

        if (focusAreaText != null)
            focusAreaText.text = $"Key Focus Area: {keyFocusArea}";

        // Clear old quest cards
        if (questCardsParent != null)
        {
            foreach (Transform child in questCardsParent)
                Destroy(child.gameObject);
        }

        // Spawn new quest cards
        if (questCardPrefab != null && questCardsParent != null)
        {
            foreach (var quest in recommendedQuests)
            {
                GameObject cardObj = Instantiate(questCardPrefab, questCardsParent);
                QuestCardUI cardUI = cardObj.GetComponent<QuestCardUI>();

                if (cardUI != null)
                {
                    cardUI.Setup(quest.title, quest.description, quest.isUnlocked, quest.isRecommended);
                }
            }
        }
    }

    private void OnContinueClicked()
    {
        OnboardingManager.Instance.EnterWorld();
    }

    private void UpdateDynamicData()
    {
        if (OnboardingQuestManager.Instance == null) return;

        int completedCount = OnboardingQuestManager.Instance.GetCompletedQuestCount();
        playerLevel = (completedCount / 2) + 1;

        // Simple logic for stage and focus area
        if (completedCount < 5)
        {
            currentStage = "Early Builder";
            keyFocusArea = "Foundation & Identity";
        }
        else if (completedCount < 8)
        {
            currentStage = "Growth Phase";
            keyFocusArea = "Leads & Sales Systems";
        }
        else
        {
            currentStage = "Scaling Master";
            keyFocusArea = "Operations & Automation";
        }
    }
}