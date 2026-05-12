using UnityEngine;
using TMPro;

public class Quest_BrickClaimScreen : QuestScreenBase
{
    [Header("Quest 1 Fields")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField companyInput;
    [SerializeField] private TMP_InputField messageInput;

    protected override void OnShow()
    {
        base.OnShow();
        questNumber = 2; // Claim Brick is now Quest 2
        questTitle = "Claim Your Brick";
        
        UpdateDefaultFields();

        // Pause the game when opening the claim brick panel
        Time.timeScale = 0f;
        Debug.Log("[BrickClaim] Game paused (timeScale = 0)");
    }

    protected override void OnHide()
    {
        base.OnHide();
        
        // Resume the game when closing the panel
        Time.timeScale = 1f;
        Debug.Log("[BrickClaim] Game resumed (timeScale = 1)");
    }

    private void UpdateDefaultFields()
    {
        if (nameInput != null && OnboardingQuestManager.Instance != null)
        {
            string username = OnboardingQuestManager.Instance.CurrentUsername;
            
            // If manager doesn't have it, try PlayerPrefs as fallback
            if (string.IsNullOrEmpty(username))
            {
                username = PlayerPrefs.GetString("OnboardingUsername", "");
            }

            nameInput.text = username;
            Debug.Log($"[BrickClaim] Pre-populated name field with: {username}");
        }
    }

    protected override void OnSubmitClicked()
    {
        string name = nameInput.text;
        string company = companyInput.text;
        string message = messageInput.text;

        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("Name is required for the brick.");
            return;
        }

        BrickData data = new BrickData { name = name, company = company, message = message };
        string json = JsonUtility.ToJson(data);

        OnboardingManager.Instance.CreateBrick(name, company, message, (success, error) =>
        {
            if (success)
            {
                CompleteQuest(json);
            }
            else
            {
                Debug.LogError($"Failed to claim brick: {error}");
            }
        });
    }

    [System.Serializable]
    private class BrickData
    {
        public string name;
        public string company;
        public string message;
    }
}
