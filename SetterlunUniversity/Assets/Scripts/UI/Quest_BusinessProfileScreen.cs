using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_BusinessProfileScreen : QuestScreenBase
{
    [Header("Steps")]
    [SerializeField] private GameObject[] steps;
    private int currentStep = 0;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_InputField businessNameInput;
    [SerializeField] private TMP_InputField businessWebsiteInput;

    [Header("Step 1: Revenue & Stage")]
    [SerializeField] private Transform revenueContainer;
    [SerializeField] private GameObject revenuePrefab;
    private string selectedRevenue;
    private List<QuestOptionChip> revenueChips = new List<QuestOptionChip>();
    // [SerializeField] private TMP_Dropdown revenueDropdown;
    [SerializeField] private string[] revenueOptions = new string[]
    {
        "$0", "$1–$5K", "$5–$10K", "$10–$25K", "$25–$50K", "$50–$100K", "$100K+"
    };

    [Header("Step 2: Business Type")]
    [SerializeField] private Transform businessTypeContainer;
    [SerializeField] private GameObject businessTypePrefab;
    [SerializeField] private string[] businessTypeOptions = new string[]
    {
        "Agency", "Coach / Consultant", "Service Provider", "Community Owner", 
        "SaaS", "AI Agency", "Content Creator", "Local Business", "E-commerce", "Not sure"
    };
    private string selectedBusinessType;
    private List<QuestOptionChip> businessTypeChips = new List<QuestOptionChip>();

    [Header("Step 3: What You Sell")]
    [SerializeField] private Transform sellContainer;
    [SerializeField] private GameObject sellPrefab;
    [SerializeField] private string[] productOptions = new string[]
    {
        "High-ticket services", "Low-ticket products", "Subscriptions", 
        "DFY services", "1:1 coaching", "Group programs", "Nothing yet"
    };
    private List<string> selectedProducts = new List<string>();
    private List<QuestOptionChip> sellChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest4Data
    {
        public string business_name;
        public string business_website;
        public string monthly_revenue;
        public string business_type;
        public string[] what_you_sell;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 4;
        questTitle = "Business Profile";

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        InitializeRevenueDropdown();
        InitializeBusinessTypes();
        InitializeSellOptions();
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

    private void InitializeRevenueDropdown()
    {
        if (revenueContainer == null || revenuePrefab == null) return;

        foreach (Transform child in revenueContainer) Destroy(child.gameObject);
        revenueChips.Clear();

        foreach (var product in revenueOptions)
        {
            GameObject chipObj = Instantiate(revenuePrefab, revenueContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();

            if (chip != null)
            {
                revenueChips.Add(chip);
                chip.Setup(product, (value) => OnRevenueSelected(chip));
            }
            else
            {
                // Robust Fallback if script is missing
                var tmpText = chipObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = product;
                else
                {
                    var legacyText = chipObj.GetComponentInChildren<Text>();
                    if (legacyText != null) legacyText.text = product;
                }

                // Hook up Button/Toggle manually for multi-selection
                var button = chipObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => 
                    {
                        if (selectedProducts.Contains(product)) selectedProducts.Remove(product);
                        else selectedProducts.Add(product);
                    });
                }
                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => 
                    {
                        if (isOn) { if (!selectedProducts.Contains(product)) selectedProducts.Add(product); }
                        else { selectedProducts.Remove(product); }
                    });
                }

                Debug.LogWarning($"QuestOptionChip missing on {sellPrefab.name}. Selection hooked via fallback.");
            }
        }
    }
    
    private void OnRevenueSelected(QuestOptionChip selectedChip)
    {
        foreach (var chip in revenueChips) chip.SetSelected(chip == selectedChip);
        selectedRevenue = selectedChip.OptionValue;
    }

    private void InitializeBusinessTypes()
    {
        if (businessTypeContainer == null || businessTypePrefab == null) return;

        foreach (Transform child in businessTypeContainer) Destroy(child.gameObject);
        businessTypeChips.Clear();

        foreach (var type in businessTypeOptions)
        {
            GameObject chipObj = Instantiate(businessTypePrefab, businessTypeContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            
            if (chip != null)
            {
                businessTypeChips.Add(chip);
                chip.Setup(type, (value) => OnBusinessTypeSelected(chip));
            }
            else
            {
                // Robust Fallback if script is missing
                var tmpText = chipObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = type;
                else
                {
                    var legacyText = chipObj.GetComponentInChildren<Text>();
                    if (legacyText != null) legacyText.text = type;
                }

                // Hook up Button/Toggle manually for single selection
                var button = chipObj.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => { selectedBusinessType = type; });
                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null) toggle.onValueChanged.AddListener((isOn) => { if (isOn) selectedBusinessType = type; });

                Debug.LogWarning($"QuestOptionChip missing on {businessTypePrefab.name}. Selection hooked via fallback.");
            }
        }
    }

    private void OnBusinessTypeSelected(QuestOptionChip selectedChip)
    {
        selectedBusinessType = selectedChip.OptionValue;
        foreach (var chip in businessTypeChips) chip.SetSelected(chip == selectedChip);
    }

    private void InitializeSellOptions()
    {
        if (sellContainer == null || sellPrefab == null) return;

        foreach (Transform child in sellContainer) Destroy(child.gameObject);
        sellChips.Clear();

        foreach (var product in productOptions)
        {
            GameObject chipObj = Instantiate(sellPrefab, sellContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();

            if (chip != null)
            {
                sellChips.Add(chip);
                chip.Setup(product, (value) => OnProductSelected(chip));
            }
            else
            {
                // Robust Fallback if script is missing
                var tmpText = chipObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = product;
                else
                {
                    var legacyText = chipObj.GetComponentInChildren<Text>();
                    if (legacyText != null) legacyText.text = product;
                }

                // Hook up Button/Toggle manually for multi-selection
                var button = chipObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => 
                    {
                        if (selectedProducts.Contains(product)) selectedProducts.Remove(product);
                        else selectedProducts.Add(product);
                    });
                }
                var toggle = chipObj.GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => 
                    {
                        if (isOn) { if (!selectedProducts.Contains(product)) selectedProducts.Add(product); }
                        else { selectedProducts.Remove(product); }
                    });
                }

                Debug.LogWarning($"QuestOptionChip missing on {sellPrefab.name}. Selection hooked via fallback.");
            }
        }
    }

    private void OnProductSelected(QuestOptionChip chip)
    {
        bool newState = !chip.IsSelected;
        chip.SetSelected(newState);

        if (newState)
        {
            if (!selectedProducts.Contains(chip.OptionValue)) selectedProducts.Add(chip.OptionValue);
        }
        else
        {
            selectedProducts.Remove(chip.OptionValue);
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
        // 1. Create the clean Quest 4 JSON data as requested
        var cleanQuestData = new Quest4Data
        {
            business_name = businessNameInput != null ? businessNameInput.text : "",
            business_website = businessWebsiteInput != null ? businessWebsiteInput.text : "",
            monthly_revenue = string.IsNullOrEmpty(selectedRevenue) ?  "0":selectedRevenue,
            business_type = selectedBusinessType,
            what_you_sell = selectedProducts.ToArray()
        };

        // 2. Prepare the data for OnboardingManager (Full Profile)
        var fullBusinessData = new OnboardingManager.BusinessProfileData
        {
            business_name = cleanQuestData.business_name,
            business_website = cleanQuestData.business_website,
            monthly_revenue = cleanQuestData.monthly_revenue,
            business_type = cleanQuestData.business_type,
            products_services = cleanQuestData.what_you_sell
        };

        // 3. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Business profile saved successfully!");
                CheckNextScreenAndExit();
            }
            else
            {
                Debug.LogError("Failed to save business profile: " + error);
            }
        });
    }
}
