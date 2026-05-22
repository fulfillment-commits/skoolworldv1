using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_OperationsScreen : QuestScreenBase
{
    [Header("Steps")]
    [SerializeField] private GameObject[] steps;
    private int currentStep = 0;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;

    [Header("Step 1: Fulfillment Issues (Multi-Select)")]
    [SerializeField] private Transform fulfillmentContainer;
    [SerializeField] private GameObject fulfillmentIssueChipPrefab;
    [SerializeField] private string[] fulfillmentOptions = new string[]
    {
        "Messy fulfillment", "Client confusion", "Manual delivery", 
        "No SOPs", "No automation", "Team issues", "I’m the bottleneck"
    };
    private List<string> selectedFulfillmentIssues = new List<string>();
    private List<QuestOptionChip> fulfillmentChips = new List<QuestOptionChip>();

    [Header("Step 2: Tools Used (Multi-Select)")]
    [SerializeField] private Transform toolsContainer;
    [SerializeField] private GameObject toolsUsedChipPrefab;
    [SerializeField] private string[] toolsOptions = new string[]
    {
        "GoHighLevel", "HubSpot", "Notion", "Airtable", "None", "Other"
    };
    private List<string> selectedTools = new List<string>();
    private List<QuestOptionChip> toolsChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest6Data
    {
        public string[] fulfillment_issues;
        public string[] tools_used;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 6;
        questTitle = "Operations";

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        InitializeFulfillmentIssues();
        InitializeToolsUsed();
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

    private void InitializeFulfillmentIssues()
    {
        if (fulfillmentContainer == null || fulfillmentIssueChipPrefab == null) return;
        foreach (Transform child in fulfillmentContainer) Destroy(child.gameObject);
        fulfillmentChips.Clear();

        foreach (var option in fulfillmentOptions)
        {
            GameObject chipObj = Instantiate(fulfillmentIssueChipPrefab, fulfillmentContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                fulfillmentChips.Add(chip);
                chip.Setup(option, (value) => OnFulfillmentIssueSelected(chip));
            }
            else
            {
                // Robust Fallback
                UpdateFallbackText(chipObj, option);
                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => 
                    {
                        if (isOn) { if (!selectedFulfillmentIssues.Contains(option)) selectedFulfillmentIssues.Add(option); }
                        else { selectedFulfillmentIssues.Remove(option); }
                    });
                }
            }
        }
    }

    private void OnFulfillmentIssueSelected(QuestOptionChip chip)
    {
        bool newState = !chip.IsSelected;
        chip.SetSelected(newState);
        if (newState)
        {
            if (!selectedFulfillmentIssues.Contains(chip.OptionValue)) selectedFulfillmentIssues.Add(chip.OptionValue);
        }
        else
        {
            selectedFulfillmentIssues.Remove(chip.OptionValue);
        }
    }

    private void InitializeToolsUsed()
    {
        if (toolsContainer == null || toolsUsedChipPrefab == null) return;
        foreach (Transform child in toolsContainer) Destroy(child.gameObject);
        toolsChips.Clear();

        foreach (var option in toolsOptions)
        {
            GameObject chipObj = Instantiate(toolsUsedChipPrefab, toolsContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                toolsChips.Add(chip);
                chip.Setup(option, (value) => OnToolsUsedSelected(chip));
            }
            else
            {
                // Robust Fallback
                UpdateFallbackText(chipObj, option);
                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => 
                    {
                        if (isOn) { if (!selectedTools.Contains(option)) selectedTools.Add(option); }
                        else { selectedTools.Remove(option); }
                    });
                }
            }
        }
    }

    private void OnToolsUsedSelected(QuestOptionChip chip)
    {
        bool newState = !chip.IsSelected;
        chip.SetSelected(newState);
        if (newState)
        {
            if (!selectedTools.Contains(chip.OptionValue)) selectedTools.Add(chip.OptionValue);
        }
        else
        {
            selectedTools.Remove(chip.OptionValue);
        }
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
        // 1. Create clean Quest 6 JSON data
        var cleanQuestData = new Quest6Data
        {
            fulfillment_issues = selectedFulfillmentIssues.ToArray(),
            tools_used = selectedTools.ToArray()
        };

        // 2. Prepare data for OnboardingManager
        var businessData = new OnboardingManager.BusinessProfileData
        {
            fulfillment_challenges = cleanQuestData.fulfillment_issues,
            tools_used = cleanQuestData.tools_used
        };

        // 3. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Operations data saved successfully!");
                CheckNextScreenAndExit();
            }
            else
            {
                Debug.LogError("Failed to save operations data: " + error);
            }
        });
    }
}
