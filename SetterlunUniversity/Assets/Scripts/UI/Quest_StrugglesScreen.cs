using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_StrugglesScreen : QuestScreenBase
{
    [Header("Struggles Config")]
    [SerializeField] private Transform strugglesContainer;
    [SerializeField] private GameObject struggleChipPrefab;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    
    [SerializeField] private string[] struggleOptions = new string[]
    {
        "Picking offer", "Standing out", "Leads", "Booking calls", 
        "Closing", "Scaling", "Fulfillment", "Hiring", 
        "Automation", "Burnout", "No roadmap"
    };

    private List<string> selectedStruggles = new List<string>();
    private List<QuestOptionChip> struggleChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest8Data
    {
        public string[] biggest_struggles;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 8;
        questTitle = "Biggest Struggles";

        if (errorMessageText != null) errorMessageText.text = "";

        InitializeStruggles();
    }

    private void InitializeStruggles()
    {
        if (strugglesContainer == null || struggleChipPrefab == null) return;

        foreach (Transform child in strugglesContainer) Destroy(child.gameObject);
        struggleChips.Clear();

        foreach (var option in struggleOptions)
        {
            GameObject chipObj = Instantiate(struggleChipPrefab, strugglesContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            
            if (chip != null)
            {
                struggleChips.Add(chip);
                chip.Setup(option, (value) => OnStruggleSelected(chip));
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
                        HandleSelectionLogic(option, isOn, () => toggle.SetIsOnWithoutNotify(false));
                    });
                }
            }
        }
    }

    private void OnStruggleSelected(QuestOptionChip chip)
    {
        bool isCurrentlySelected = chip.IsSelected;
        HandleSelectionLogic(chip.OptionValue, !isCurrentlySelected, () => chip.SetSelected(false));
        
        // Refresh visuals for the clicked chip based on updated list
        chip.SetSelected(selectedStruggles.Contains(chip.OptionValue));
    }

    private void HandleSelectionLogic(string value, bool tryingToSelect, System.Action onSelectionDenied)
    {
        if (tryingToSelect)
        {
            if (selectedStruggles.Count < 3)
            {
                if (!selectedStruggles.Contains(value)) selectedStruggles.Add(value);
                if (errorMessageText != null) errorMessageText.text = "";
            }
            else
            {
                if (errorMessageText != null) errorMessageText.text = "You can select max 3 options";
                onSelectionDenied?.Invoke();
            }
        }
        else
        {
            selectedStruggles.Remove(value);
            if (errorMessageText != null) errorMessageText.text = "";
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

    protected override void OnSubmitClicked()
    {
        if (selectedStruggles.Count == 0)
        {
            if (errorMessageText != null) errorMessageText.text = "Please select at least 1 option";
            return;
        }
        
        SaveAndComplete();
    }

    private void SaveAndComplete()
    {
        // 1. Create clean Quest 8 JSON data
        var cleanQuestData = new Quest8Data
        {
            biggest_struggles = selectedStruggles.ToArray()
        };

        // 2. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Struggles data saved successfully!");
                OnExitClicked();
            }
            else
            {
                Debug.LogError("Failed to save struggles data: " + error);
            }
        });
    }
}
