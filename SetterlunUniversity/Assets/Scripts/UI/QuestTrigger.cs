using UnityEngine;
using UnityEngine.Events;
using System;

public class QuestTrigger : MonoBehaviour
{
    [Header("Quest Configuration")]
    public int questNumber;
    public ScreenType questScreenType;
    public string questTitle = "Quest Title";
    
    [Header("Before Completion")]
    public string interactionButtonText = "Claim your Brick";
    public UnityEvent onEnterBeforeCompletion;
    public UnityEvent onExitBeforeCompletion;
    public UnityEvent onInteractionClickBeforeCompletion;

    [Header("After Completion")]
    public bool isShowTriggerAfterCompletion = false;
    public string afterCompletionButtonText = "Completed";
    public UnityEvent onEnterAfterCompletion;
    public UnityEvent onExitAfterCompletion;
    public UnityEvent onInteractionClickAfterCompletion;

    private bool playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            InvokeTriggerEvents(true);
            UpdateInteractionUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            InvokeTriggerEvents(false);
            UpdateInteractionUI();
        }
    }

    private void InvokeTriggerEvents(bool entering)
    {
        bool isCompleted = OnboardingQuestManager.Instance != null && OnboardingQuestManager.Instance.IsQuestCompleted(questNumber);
        
        Debug.Log($"[QuestTrigger] Quest {questNumber} isCompleted: {isCompleted}. Entering: {entering}");

        if (entering)
        {
            if (!isCompleted)
            {
                Debug.Log($"[QuestTrigger] Invoking onEnterBeforeCompletion for Quest {questNumber}");
                onEnterBeforeCompletion?.Invoke();
            }
            else if (isShowTriggerAfterCompletion)
            {
                Debug.Log($"[QuestTrigger] Invoking onEnterAfterCompletion for Quest {questNumber}");
                onEnterAfterCompletion?.Invoke();
            }
        }
        else
        {
            if (!isCompleted)
                onExitBeforeCompletion?.Invoke();
            else if (isShowTriggerAfterCompletion)
                onExitAfterCompletion?.Invoke();
        }
    }

    private void UpdateInteractionUI()
    {
        if (playerInside)
        {
            bool isCompleted = OnboardingQuestManager.Instance != null && OnboardingQuestManager.Instance.IsQuestCompleted(questNumber);
            
            if (!isCompleted)
            {
                // Sequencing: Check if previous quest is completed
                if (questNumber > 1 && !OnboardingQuestManager.Instance.IsQuestCompleted(questNumber - 1))
                {
                    QuestInteractionController.OnRequestHide?.Invoke(questNumber);
                    return;
                }

                // Only pass callback if something is assigned in the inspector, otherwise let controller handle default screen
                Action callback = (onInteractionClickBeforeCompletion.GetPersistentEventCount() > 0) ? () => onInteractionClickBeforeCompletion?.Invoke() : null;
                QuestInteractionController.OnRequestShow?.Invoke(questNumber, questScreenType, questTitle, interactionButtonText, callback);
            }
            else if (isShowTriggerAfterCompletion)
            {
                // For after completion, we almost always want the custom callback (e.g., show brick details)
                Action callback = (onInteractionClickAfterCompletion.GetPersistentEventCount() > 0) ? () => onInteractionClickAfterCompletion?.Invoke() : null;
                QuestInteractionController.OnRequestShow?.Invoke(questNumber, questScreenType, questTitle, afterCompletionButtonText, callback);
            }
            else
            {
                QuestInteractionController.OnRequestHide?.Invoke(questNumber);
            }
        }
        else
        {
            QuestInteractionController.OnRequestHide?.Invoke(questNumber);
        }
    }

    public void ForceReTrigger()
    {
        UpdateInteractionUI();
    }
}
