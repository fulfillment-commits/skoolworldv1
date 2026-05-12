using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_LeadsSalesScreen : QuestScreenBase
{
    [Header("Steps")]
    [SerializeField] private GameObject[] steps;
    private int currentStep = 0;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;

    [Header("Step 1: Lead Sources (Multi-Select)")]
    [SerializeField] private Transform leadSourcesContainer;
    [SerializeField] private GameObject leadSourceChipPrefab;
    [SerializeField] private string[] leadSourceOptions = new string[]
    {
        "Paid ads", "Outbound DMs", "Cold email", "Organic/content", 
        "Referrals", "Partnerships", "No consistent leads"
    };
    private List<string> selectedLeadSources = new List<string>();
    private List<QuestOptionChip> leadSourceChips = new List<QuestOptionChip>();

    [Header("Step 2: Biggest Lead Issue (Single-Select)")]
    [SerializeField] private Transform leadIssueContainer;
    [SerializeField] private GameObject leadIssueCardPrefab;
    [SerializeField] private string[] leadIssueOptions = new string[]
    {
        "Not enough leads", "Wrong leads", "Leads don’t book", 
        "Ghosting", "Don’t close", "No system"
    };
    private string selectedLeadIssue;
    private List<QuestOptionChip> leadIssueChips = new List<QuestOptionChip>();

    [Header("Step 3: Sales Process (Multi-Select)")]
    [SerializeField] private Transform salesProcessContainer;
    [SerializeField] private GameObject salesProcessChipPrefab;
    [SerializeField] private string[] salesProcessOptions = new string[]
    {
        "Daily calls", "Occasionally", "Want to start", 
        "Selling via DMs", "No process"
    };
    private List<string> selectedSalesProcesses = new List<string>();
    private List<QuestOptionChip> salesProcessChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest5Data
    {
        public string[] lead_sources;
        public string lead_issue;
        public string[] sales_process;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 5;
        questTitle = "Leads & Sales";

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        InitializeLeadSources();
        InitializeLeadIssues();
        InitializeSalesProcesses();
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

    private void InitializeLeadSources()
    {
        if (leadSourcesContainer == null || leadSourceChipPrefab == null) return;
        foreach (Transform child in leadSourcesContainer) Destroy(child.gameObject);
        leadSourceChips.Clear();

        foreach (var option in leadSourceOptions)
        {
            GameObject chipObj = Instantiate(leadSourceChipPrefab, leadSourcesContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                leadSourceChips.Add(chip);
                chip.Setup(option, (value) => OnLeadSourceSelected(chip));
            }
            else
            {
                // Robust Fallback: Search all children (including inactive) for text components
                var tmpText = chipObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpText.Length > 0) foreach (var t in tmpText) t.text = option;
                else
                {
                    var legacyText = chipObj.GetComponentsInChildren<Text>(true);
                    foreach (var t in legacyText) t.text = option;
                }

                // If it has a toggle, try to hook it up manually
                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => 
                    {
                        if (isOn) { if (!selectedLeadSources.Contains(option)) selectedLeadSources.Add(option); }
                        else { selectedLeadSources.Remove(option); }
                    });
                }
            }
        }
    }

    private void OnLeadSourceSelected(QuestOptionChip chip)
    {
        bool newState = !chip.IsSelected;
        chip.SetSelected(newState);
        if (newState)
        {
            if (!selectedLeadSources.Contains(chip.OptionValue)) selectedLeadSources.Add(chip.OptionValue);
        }
        else
        {
            selectedLeadSources.Remove(chip.OptionValue);
        }
    }

    private void InitializeLeadIssues()
    {
        if (leadIssueContainer == null || leadIssueCardPrefab == null) return;
        foreach (Transform child in leadIssueContainer) Destroy(child.gameObject);
        leadIssueChips.Clear();

        foreach (var option in leadIssueOptions)
        {
            GameObject chipObj = Instantiate(leadIssueCardPrefab, leadIssueContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                leadIssueChips.Add(chip);
                chip.Setup(option, (value) => OnLeadIssueSelected(chip));
            }
            else
            {
                // Robust Fallback
                var tmpText = chipObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpText.Length > 0) foreach (var t in tmpText) t.text = option;
                else
                {
                    var legacyText = chipObj.GetComponentsInChildren<Text>(true);
                    foreach (var t in legacyText) t.text = option;
                }
            }
        }
    }

    private void OnLeadIssueSelected(QuestOptionChip selectedChip)
    {
        selectedLeadIssue = selectedChip.OptionValue;
        foreach (var chip in leadIssueChips) chip.SetSelected(chip == selectedChip);
    }

    private void InitializeSalesProcesses()
    {
        if (salesProcessContainer == null || salesProcessChipPrefab == null) return;
        foreach (Transform child in salesProcessContainer) Destroy(child.gameObject);
        salesProcessChips.Clear();

        foreach (var option in salesProcessOptions)
        {
            GameObject chipObj = Instantiate(salesProcessChipPrefab, salesProcessContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                salesProcessChips.Add(chip);
                chip.Setup(option, (value) => OnSalesProcessSelected(chip));
            }
            else
            {
                // Robust Fallback
                var tmpText = chipObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmpText.Length > 0) foreach (var t in tmpText) t.text = option;
                else
                {
                    var legacyText = chipObj.GetComponentsInChildren<Text>(true);
                    foreach (var t in legacyText) t.text = option;
                }

                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => 
                    {
                        if (isOn) { if (!selectedSalesProcesses.Contains(option)) selectedSalesProcesses.Add(option); }
                        else { selectedSalesProcesses.Remove(option); }
                    });
                }
            }
        }
    }

    private void OnSalesProcessSelected(QuestOptionChip chip)
    {
        bool newState = !chip.IsSelected;
        chip.SetSelected(newState);
        if (newState)
        {
            if (!selectedSalesProcesses.Contains(chip.OptionValue)) selectedSalesProcesses.Add(chip.OptionValue);
        }
        else
        {
            selectedSalesProcesses.Remove(chip.OptionValue);
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
        // 1. Create clean Quest 5 JSON data
        var cleanQuestData = new Quest5Data
        {
            lead_sources = selectedLeadSources.ToArray(),
            lead_issue = selectedLeadIssue,
            sales_process = selectedSalesProcesses.ToArray()
        };

        // 2. Prepare data for OnboardingManager (Business Profile update)
        var businessData = new OnboardingManager.BusinessProfileData
        {
            lead_sources = cleanQuestData.lead_sources,
            sales_issues = new string[] { cleanQuestData.lead_issue },
            sales_process_status = string.Join(", ", cleanQuestData.sales_process)
        };

        // 3. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Leads & Sales data saved successfully!");
                OnExitClicked();
            }
            else
            {
                Debug.LogError("Failed to save leads & sales data: " + error);
            }
        });
    }
}
