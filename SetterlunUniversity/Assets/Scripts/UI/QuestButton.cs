using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Toggle toggle;

    private int questNumber;
    private QuestPanelUI parentPanel;

    // This will be set by QuestPanelUI
    public void Initialize(int number, string title, QuestPanelUI panel)
    {
        questNumber = number;
        titleText.text = title;
        parentPanel = panel;

        // Ensure the toggle is visually correct but non-interactable for manual clicks
        // because we want the whole button area to trigger the quest screen
        toggle.interactable = false; 

        // Add a button component if not already there, or use a transparent button over the top
        // to detect the click to open the quest screen.
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(HandleClick);
        }
    }

    public void SetCompleted(bool completed)
    {
        if (toggle != null)
        {
            toggle.isOn = completed;
        }

        // If completed, we can disable the click button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = !completed;
        }
    }

    private void HandleClick()
    {
        if (parentPanel != null)
        {
            parentPanel.OnQuestButtonClicked(questNumber);
        }
    }
}