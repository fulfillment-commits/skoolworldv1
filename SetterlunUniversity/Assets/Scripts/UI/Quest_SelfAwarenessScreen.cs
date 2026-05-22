using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class Quest_SelfAwarenessScreen : QuestScreenBase
{
    [Header("UI Elements")]
    [SerializeField] private Transform awarenessContainer;
    [SerializeField] private GameObject awarenessChipPrefab;
    [SerializeField] private TextMeshProUGUI errorMessageText;

    [Header("Awareness Options")]
    [SerializeField] private string[] awarenessOptions = new string[]
    {
        "Need clarity", "Need systems", "Need leads", 
        "Need sales", "Need scale", "Overwhelmed"
    };

    private List<string> selectedAwareness = new List<string>();
    private List<QuestOptionChip> awarenessChips = new List<QuestOptionChip>();

    [System.Serializable]
    private class Quest10Data
    {
        public string[] self_awareness;
    }

    protected override void Start()
    {
        base.Start();
        questNumber = 10;
        questTitle = "Self-Awareness";

        if (errorMessageText != null) errorMessageText.text = "";

        InitializeAwarenessOptions();
    }

    private void InitializeAwarenessOptions()
    {
        if (awarenessContainer == null || awarenessChipPrefab == null) return;

        foreach (Transform child in awarenessContainer) Destroy(child.gameObject);
        awarenessChips.Clear();

        foreach (var option in awarenessOptions)
        {
            GameObject chipObj = Instantiate(awarenessChipPrefab, awarenessContainer);
            QuestOptionChip chip = chipObj.GetComponent<QuestOptionChip>();
            
            if (chip != null)
            {
                awarenessChips.Add(chip);
                chip.Setup(option, (value) => OnAwarenessSelected(chip));
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
                        if (isOn)
                        {
                            if (!selectedAwareness.Contains(option)) selectedAwareness.Add(option);
                        }
                        else
                        {
                            selectedAwareness.Remove(option);
                        }
                    });
                }
            }
        }
    }

    private void OnAwarenessSelected(QuestOptionChip chip)
    {
        bool newState = !chip.IsSelected;
        chip.SetSelected(newState);

        if (newState)
        {
            if (!selectedAwareness.Contains(chip.OptionValue)) selectedAwareness.Add(chip.OptionValue);
        }
        else
        {
            selectedAwareness.Remove(chip.OptionValue);
        }
        
        if (errorMessageText != null) errorMessageText.text = "";
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
        if (selectedAwareness.Count == 0)
        {
            if (errorMessageText != null) errorMessageText.text = "Please select at least 1 option";
            return;
        }
        
        SaveAndComplete();
    }

    private void SaveAndComplete()
    {
        // 1. Create clean Quest 10 JSON data
        var cleanQuestData = new Quest10Data
        {
            self_awareness = selectedAwareness.ToArray()
        };

        // 2. Save to Firebase and Local Quest Log
        OnboardingManager.Instance.CompleteQuest(questNumber, JsonUtility.ToJson(cleanQuestData), (success, error) =>
        {
            if (success)
            {
                Debug.Log("✅ Self-Awareness data saved successfully!");
                CheckNextScreenAndExit();
            }
            else
            {
                Debug.LogError("Failed to save awareness data: " + error);
            }
        });
    }
}
