using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image statusIcon;
    [SerializeField] private GameObject recommendedBadge;

    public void Setup(string title, string description, bool unlocked, bool recommended)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        if (statusIcon != null)
            statusIcon.color = unlocked ? Color.green : new Color(0.5f, 0.5f, 0.5f);

        if (recommendedBadge != null)
            recommendedBadge.SetActive(recommended);
    }
}