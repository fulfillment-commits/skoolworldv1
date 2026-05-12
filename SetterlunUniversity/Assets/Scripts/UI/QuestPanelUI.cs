using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestPanelUI : MonoBehaviour
{
    public static QuestPanelUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private QuestButton[] questButtons;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Automatically register with Quest Manager if it exists
        if (OnboardingQuestManager.Instance != null)
        {
            OnboardingQuestManager.Instance.RegisterQuestPanel(this);
        }
    }

    public void Refresh()
    {
        if (OnboardingQuestManager.Instance == null) return;

        float progress = OnboardingQuestManager.Instance.GetOverallProgress();
        progressBar.value = progress;
        progressText.text = $"{Mathf.RoundToInt(progress * 100)}% Complete";

        for (int i = 0; i < questButtons.Length; i++)
        {
            bool completed = OnboardingQuestManager.Instance.IsQuestCompleted(i + 1);
            questButtons[i].SetCompleted(completed);
        }
    }

    public void OnQuestButtonClicked(int questNumber)
    {
        if (OnboardingQuestManager.Instance.IsQuestCompleted(questNumber))
            return;

        // Sequencing: Check if previous quest is completed
        if (questNumber > 1 && !OnboardingQuestManager.Instance.IsQuestCompleted(questNumber - 1))
        {
            Debug.LogWarning($"[QuestPanelUI] Quest {questNumber} is locked. Complete Quest {questNumber - 1} first.");
            return;
        }

        ScreenType target = GetScreenForQuest(questNumber);
        ScreenManager.Instance.ShowScreen(target);
    }

    private ScreenType GetScreenForQuest(int questNumber)
    {
        return questNumber switch
        {
            1 => ScreenType.Quest_Tour,
            2 => ScreenType.Quest_BrickClaim,
            3 => ScreenType.Quest_PersonalProfile,
            4 => ScreenType.Quest_BusinessProfile,
            5 => ScreenType.Quest_LeadsSales,
            6 => ScreenType.Quest_Operations,
            7 => ScreenType.Quest_Authority,
            8 => ScreenType.Quest_Struggles,
            9 => ScreenType.Quest_Goals,
            10 => ScreenType.Quest_SelfAwareness,
            _ => ScreenType.MainWorld
        };
    }
}