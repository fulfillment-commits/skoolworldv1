using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_GoalsScreen : QuestScreenBase
{
    [Header("Steps")]
    [SerializeField] private GameObject[] steps;
    private int currentStep = 0;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;

    [Header("Step 1: 90-Day Goal (Single-Select)")]
    [SerializeField] private Transform goalContainer;
    [SerializeField] private GameObject goalCardPrefab;
    [SerializeField] private string[] goalOptions = new string[]
    {
        "First clients", "$10K/month", "$25K/month", "$50K+", 
        "Reduce workload", "Build authority", "Build systems", "Launch offer"
    };
    private string selectedGoal;
    private List<QuestOptionChip> goalChips = new List<QuestOptionChip>();

    [Header("Step 2: Time Commitment (Single-Select)")]
    [SerializeField] private Transform timeContainer;
    [SerializeField] private GameObject timeCardPrefab;
    [SerializeField] private string[] timeOptions = new string[]
    {
        "<3 hrs", "3–5 hrs", "5–10 hrs", "10+ hrs"
    };
    private string selectedTime;
    private List<QuestOptionChip> timeChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest9Data
    {
        public string primary_goal_90_day;
        public string time_commitment;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 9;
        questTitle = "Goals";

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        InitializeGoals();
        InitializeTimeCommitment();
        UpdateStepUI();
    }

    private void OnBackClicked()
    {
        if (currentStep > 0)
        {
            currentStep--;
            UpdateStepUI();
        }
    }

    private void InitializeGoals()
    {
        if (goalContainer == null || goalCardPrefab == null) return;
        foreach (Transform child in goalContainer) Destroy(child.gameObject);
        goalChips.Clear();

        foreach (var option in goalOptions)
        {
            GameObject chipObj = Instantiate(goalCardPrefab, goalContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                goalChips.Add(chip);
                chip.Setup(option, (value) => OnGoalSelected(chip));
            }
            else
            {
                // Robust Fallback
                UpdateFallbackText(chipObj, option);
                var button = chipObj.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => { selectedGoal = option; });
            }
        }
    }

    private void OnGoalSelected(QuestOptionChip selectedChip)
    {
        selectedGoal = selectedChip.OptionValue;
        foreach (var chip in goalChips) chip.SetSelected(chip == selectedChip);
    }

    private void InitializeTimeCommitment()
    {
        if (timeContainer == null || timeCardPrefab == null) return;
        foreach (Transform child in timeContainer) Destroy(child.gameObject);
        timeChips.Clear();

        foreach (var option in timeOptions)
        {
            GameObject chipObj = Instantiate(timeCardPrefab, timeContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                timeChips.Add(chip);
                chip.Setup(option, (value) => OnTimeSelected(chip));
            }
            else
            {
                // Robust Fallback
                UpdateFallbackText(chipObj, option);
                var button = chipObj.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => { selectedTime = option; });
            }
        }
    }

    private void OnTimeSelected(QuestOptionChip selectedChip)
    {
        selectedTime = selectedChip.OptionValue;
        foreach (var chip in timeChips) chip.SetSelected(chip == selectedChip);
    }

    private void UpdateFallbackText(GameObject obj, string text)
    {
        var tmpText = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (tmpText.Length > 0) foreach (var t in tmpText) t.text = text;
        else
        {
            var legacyText = obj.GetComponentsInChildren<Text>(true);
            foreach (var t in legacyText) t.text = text;
        }
    }

    private void UpdateStepUI()
    {
        if (steps == null || steps.Length == 0) return;
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i] != null) steps[i].SetActive(i == currentStep);
        }

        if (backButton != null) backButton.gameObject.SetActive(currentStep > 0);

        if (submitButton != null)
        {
            var text = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = (currentStep == steps.Length - 1) ? "Submit" : "Next";
        }
    }

    protected override void OnSubmitClicked()
    {
        if (currentStep < steps.Length - 1)
        {
            currentStep++;
            UpdateStepUI();
        }
        else
        {
            SaveAndComplete();
        }
    }

    private void SaveAndComplete()
    {
        // 1. Create clean Quest 9 JSON data
        var cleanQuestData = new Quest9Data
        {
            primary_goal_90_day = selectedGoal,
            time_commitment = selectedTime
        };

        // 2. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Goals data saved successfully!");
                CheckNextScreenAndExit();
            }
            else
            {
                Debug.LogError("Failed to save goals data: " + error);
            }
        });
    }
}
