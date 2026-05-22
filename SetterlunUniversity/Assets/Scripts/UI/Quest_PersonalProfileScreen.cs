using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_PersonalProfileScreen : QuestScreenBase
{
    [Header("Steps")]
    [SerializeField] private GameObject[] steps;
    private int currentStep = 0;

    [Header("UI Elements")]
    [SerializeField] private Button backButton;

    [Header("Step 1: Basic Identity")]
    [SerializeField] private TMP_InputField fullNameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField timezoneInput;
    [SerializeField] private TMP_InputField cityInput;
    [SerializeField] private TMP_InputField countryInput;

    [Header("Step 2: Public Profile")]
    [SerializeField] private TMP_InputField bioInput;
    [SerializeField] private TMP_InputField websiteInput;
    [SerializeField] private TMP_InputField phoneInput;
    [SerializeField] private Toggle visibilityToggle; // Public/Private

    [Header("Step 3: Skills")]
    [SerializeField] private Transform skillsContainer;
    [SerializeField] private GameObject skillChipPrefab;
    private List<string> selectedSkills = new List<string>();
    private string[] availableSkills = new string[]
    {
        "Ads", "SEO", "Organic Marketing", "Content Creation", "Copywriting",
        "Email Marketing", "Funnels & Landing Pages", "Webinar Marketing",
        "Video Production", "Podcasting", "Influencer Marketing", "Social Media Marketing",
        "AI Agents", "AI Automation", "Prompt Engineering", "No-Code / Low-Code",
        "CRM & Automations", "Web Development", "Offer Creation", "Sales & Closing",
        "E-commerce", "Coaching / Consulting", "SaaS", "Community Building",
        "Branding", "Business Strategy", "Finance & Operations"
    };

    [Header("Step 4: Experience")]
    [SerializeField] private Transform xpContainer;
    [SerializeField] private GameObject xpChipPrefab;
    private string selectedXP = null;
    private List<QuestOptionChip> xpChips = new List<QuestOptionChip>();
    // [SerializeField] private TMP_Dropdown experienceDropdown;
    private string[] availableXP = new string[]
    {
        "Not started", "< 6 months","6–12 months","1–3 years","3+ years"
    };
    
    protected override void Start()
    {
        base.Start();
        questNumber = 3;
        questTitle = "Personal Profile";
        
        // Pre-fill email and timezone
        if (OnboardingManager.Instance != null)
        {
            emailInput.text = OnboardingManager.Instance.CurrentEmail;
            timezoneInput.text = System.TimeZoneInfo.Local.DisplayName;
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        InitializeSkills();
        InitializeXPDropdown();
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

    private void InitializeSkills()
    {
        foreach (var skill in availableSkills)
        {
            GameObject chip = Instantiate(skillChipPrefab, skillsContainer);
            var text = chip.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = skill;
            }
            else
            {
                var tmp= chip.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = skill;
            }

            var toggle = chip.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) selectedSkills.Add(skill);
                    else selectedSkills.Remove(skill);
                });
            }
        }
    }
 private void InitializeXPDropdown()
    {
        if (xpContainer == null || xpChipPrefab == null) return;

        foreach (Transform child in xpContainer) Destroy(child.gameObject);
        xpChips.Clear();

        foreach (var product in availableXP)
        {
            GameObject chipObj = Instantiate(xpChipPrefab, xpContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();

            if (chip != null)
            {
                xpChips.Add(chip);
                chip.Setup(product, (value) => OnRevenueSelected(chip));
            }
        }
    }
    
    private void OnRevenueSelected(QuestOptionChip selectedChip)
    {
        foreach (var chip in xpChips) chip.SetSelected(chip == selectedChip);
        selectedXP = selectedChip.OptionValue;
    }
    private void UpdateStepUI()
    {
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].SetActive(i == currentStep);
        }

        // Toggle back button visibility (hide on Step 1)
        if (backButton != null)
        {
            backButton.gameObject.SetActive(currentStep > 0);
        }

        if (submitButton != null)
        {
            var text = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = (currentStep == steps.Length - 1) ? "Submit" : "Next";
            }
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
        var profileData = new OnboardingManager.PersonalProfileData
        {
            full_name = fullNameInput.text,
            email = emailInput.text,
            timezone = timezoneInput.text,
            city = cityInput.text,
            country = countryInput.text,
            bio = bioInput.text,
            website = websiteInput.text,
            phone = phoneInput.text,
            visibility = visibilityToggle.isOn ? "public" : "private",
            skills = selectedSkills.ToArray(),
            years_in_business = string.IsNullOrEmpty(selectedXP) ?  "Not started":selectedXP
        };

        OnboardingManager.Instance.CreatePersonalProfile(profileData, (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Personal profile saved successfully!");
                // Hide this screen and return to world
                if (ScreenManager.Instance != null)
                {
                    ScreenManager.Instance.ShowScreen(ScreenType.MainWorld);
                }
            }
            else
            {
                Debug.LogError("Failed to save personal profile: " + error);
            }
        });
    }
}
