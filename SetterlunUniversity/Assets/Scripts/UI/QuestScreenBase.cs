using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class QuestScreenBase : ScreenBase
{
    [Header("Quest Info")]
    [SerializeField] protected int questNumber;
    [SerializeField] protected string questTitle;

    [Header("UI Elements")]
    [SerializeField] protected TextMeshProUGUI titleText;
    [SerializeField] protected Button submitButton;
    [SerializeField] protected Button exitButton;

    [Header("Quest Trigger Reference (optional)")]
    [SerializeField] protected QuestTrigger linkedQuestTrigger;
    
    [Header("Force Next Screen")]
    [SerializeField] protected ScreenType nextScreen =ScreenType.None;

    protected virtual void Start()
    {
        if (titleText != null) titleText.text = questTitle;
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
    }

    

    protected abstract void OnSubmitClicked();

    protected virtual void CheckNextScreenAndExit()
    {
        if (nextScreen != ScreenType.None)
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.ShowScreen(nextScreen);
        }
        else
        {
            OnExitClicked();
        }
    }

    protected virtual void OnExitClicked()
    {
        
            if (linkedQuestTrigger != null)
            {
                linkedQuestTrigger.ForceReTrigger();
            }

            ScreenManager.Instance.ShowScreen(ScreenType.MainWorld);
    }

    protected void CompleteQuest(string dataJson = null)
    {
        OnboardingQuestManager.Instance.CompleteQuest(questNumber, dataJson);
        OnExitClicked();
    }
}
