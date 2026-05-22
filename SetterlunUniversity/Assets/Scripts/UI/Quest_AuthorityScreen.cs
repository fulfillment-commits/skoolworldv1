using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_AuthorityScreen : QuestScreenBase
{
    [Header("Steps")]
    [SerializeField] private GameObject[] steps;
    private int currentStep = 0;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;

    [Header("Step 1: Authority Level (Single-Select)")]
    [SerializeField] private Transform authorityLevelContainer;
    [SerializeField] private GameObject authorityLevelCardPrefab;
    [SerializeField] private string[] authorityLevelOptions = new string[]
    {
        "Unknown", "Some credibility", "Recognized expert", "Strong brand", "Media featured"
    };
    private string selectedAuthorityLevel;
    private List<QuestOptionChip> authorityLevelChips = new List<QuestOptionChip>();

    [Header("Step 2: Building Authority? (Single-Select)")]
    [SerializeField] private Transform buildingAuthorityContainer;
    [SerializeField] private GameObject buildingAuthorityCardPrefab;
    [SerializeField] private string[] buildingAuthorityOptions = new string[]
    {
        "Yes", "No", "Not sure"
    };
    private string selectedBuildingAuthority;
    private List<QuestOptionChip> buildingAuthorityChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest7Data
    {
        public string authority_level;
        public string building_authority;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 7;
        questTitle = "Authority";

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        InitializeAuthorityLevels();
        InitializeBuildingAuthority();
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

    private void InitializeAuthorityLevels()
    {
        if (authorityLevelContainer == null || authorityLevelCardPrefab == null) return;
        foreach (Transform child in authorityLevelContainer) Destroy(child.gameObject);
        authorityLevelChips.Clear();

        foreach (var option in authorityLevelOptions)
        {
            GameObject chipObj = Instantiate(authorityLevelCardPrefab, authorityLevelContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                authorityLevelChips.Add(chip);
                chip.Setup(option, (value) => OnAuthorityLevelSelected(chip));
            }
            else
            {
                // Robust Fallback
                UpdateFallbackText(chipObj, option);
            }
        }
    }

    private void OnAuthorityLevelSelected(QuestOptionChip selectedChip)
    {
        selectedAuthorityLevel = selectedChip.OptionValue;
        foreach (var chip in authorityLevelChips) chip.SetSelected(chip == selectedChip);
    }

    private void InitializeBuildingAuthority()
    {
        if (buildingAuthorityContainer == null || buildingAuthorityCardPrefab == null) return;
        foreach (Transform child in buildingAuthorityContainer) Destroy(child.gameObject);
        buildingAuthorityChips.Clear();

        foreach (var option in buildingAuthorityOptions)
        {
            GameObject chipObj = Instantiate(buildingAuthorityCardPrefab, buildingAuthorityContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            if (chip != null)
            {
                buildingAuthorityChips.Add(chip);
                chip.Setup(option, (value) => OnBuildingAuthoritySelected(chip));
            }
            else
            {
                // Robust Fallback
                UpdateFallbackText(chipObj, option);
            }
        }
    }

    private void OnBuildingAuthoritySelected(QuestOptionChip selectedChip)
    {
        selectedBuildingAuthority = selectedChip.OptionValue;
        foreach (var chip in buildingAuthorityChips) chip.SetSelected(chip == selectedChip);
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
        // 1. Create clean Quest 7 JSON data
        var cleanQuestData = new Quest7Data
        {
            authority_level = selectedAuthorityLevel,
            building_authority = selectedBuildingAuthority
        };

        // 2. Prepare data for OnboardingManager
        var businessData = new OnboardingManager.BusinessProfileData
        {
            authority_level = cleanQuestData.authority_level,
            active_authority_building = cleanQuestData.building_authority == "Yes"
        };

        // 3. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Authority data saved successfully!");
                CheckNextScreenAndExit();
            }
            else
            {
                Debug.LogError("Failed to save authority data: " + error);
            }
        });
    }
}
