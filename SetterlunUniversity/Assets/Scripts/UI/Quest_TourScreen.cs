using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[System.Serializable]
public class TourStep
{
    public string message;
    public GameObject[] objectsToActivate;
    public GameObject[] objectsToDeactivate;
}

public class Quest_TourScreen : QuestScreenBase
{
    private static Quest_TourScreen instance;

    [Header("Tour Steps")]
    [SerializeField] private List<TourStep> tourSteps = new List<TourStep>();

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI stepIndicatorText;

    private int currentStepIndex = 0;
    private bool hasStartedTour = false;
    private bool isQuestComplete = false;

    protected override void Start()
    {
        base.Start();
        instance = this;
        questNumber = 1; // Tour is now Quest 1
        questTitle = "Guided Tour";

        SetupDefaultTourSteps();
        UpdateUI();
    }

    protected override void OnShow()
    {
        base.OnShow();
        ResetTour();
        ShowWelcomeMessage();
    }

    public static void NotifyActionCompleted()
    {
        if (instance != null)
        {
            instance.AdvanceToNextStep();
        }
    }

    private void SetupDefaultTourSteps()
    {
        if (tourSteps.Count == 0)
        {
            tourSteps.Add(new TourStep { message = "Welcome to Setterlun University! This tour will show you around." });
            tourSteps.Add(new TourStep { message = "Look around! This is your campus, full of builders like you." });
            tourSteps.Add(new TourStep { message = "Learn to move and interact to find your place in the university." });
            tourSteps.Add(new TourStep { message = "This is your brick area! This is where you will claim your brick." });
            tourSteps.Add(new TourStep { message = "Tour complete! You're ready to start your journey." });
        }
    }

    private void ResetTour()
    {
        currentStepIndex = 0;
        hasStartedTour = false;
        isQuestComplete = false;

        DeactivateAllStepObjects();
    }

    private void DeactivateAllStepObjects()
    {
        foreach (var step in tourSteps)
        {
            ToggleStepObjects(step, false);
        }
    }

    private void ToggleStepObjects(TourStep step, bool active)
    {
        if (step.objectsToActivate != null)
        {
            foreach (var obj in step.objectsToActivate)
            {
                if (obj != null) obj.SetActive(active);
            }
        }

        // For deactivation, we usually want them to be the opposite of the activation state
        // if we are resetting. But specifically during a step, we follow the step rules.
        if (!active && step.objectsToDeactivate != null)
        {
            // When resetting (active=false), we don't necessarily want to touch objectsToDeactivate
            // unless your logic requires it. Usually, we just hide everything that was shown.
        }
    }

    private void ShowWelcomeMessage()
    {
        if (DynamicMessagePanel.Instance != null)
        {
            DynamicMessagePanel.Instance.ShowMessage(
                "Welcome to Setterlun University! Ready to start the tour?",
                "Let's Go!",
                OnWelcomeMessageClosed
            );
        }
        else
        {
            OnWelcomeMessageClosed();
        }
    }

    private void OnWelcomeMessageClosed()
    {
        hasStartedTour = true;
        currentStepIndex = 0;
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= tourSteps.Count)
        {
            CompleteTour();
            return;
        }

        // Activate current step UI objects
        var currentStep = tourSteps[currentStepIndex];
        if (currentStep.objectsToActivate != null)
        {
            foreach (var obj in currentStep.objectsToActivate)
                if (obj != null) obj.SetActive(true);
        }
        if (currentStep.objectsToDeactivate != null)
        {
            foreach (var obj in currentStep.objectsToDeactivate)
                if (obj != null) obj.SetActive(false);
        }

        UpdateUI();

        if (DynamicMessagePanel.Instance != null)
        {
            string message = currentStep.message;
            string buttonText = (currentStepIndex == tourSteps.Count - 1) ? "Finish" : "Next Step";
            DynamicMessagePanel.Instance.ShowMessage(message, buttonText, OnStepMessageClosed);
        }
        else
        {
            // Fallback if no message panel
            AdvanceToNextStep();
        }
    }

    private void OnStepMessageClosed()
    {
        AdvanceToNextStep();
    }

    private void AdvanceToNextStep()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    private void CompleteTour()
    {
        if (isQuestComplete) return;
        isQuestComplete = true;

        DeactivateAllStepObjects();

        // Complete Quest 1 (Brick Claim) automatically as requested
        if (OnboardingQuestManager.Instance != null)
        {
            OnboardingQuestManager.Instance.CompleteQuest(1, "{\"claimed\":true}");
        }

        // Complete Quest 2 (Tour)
        CompleteQuest("{}");
    }

    private void UpdateUI()
    {
        if (stepIndicatorText != null)
        {
            stepIndicatorText.text = $"Step {currentStepIndex + 1} of {tourSteps.Count}";
        }
    }

    protected override void OnSubmitClicked()
    {
        // Not used in simple tour
    }
}
