using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;

public class QuestOptionChip : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI labelTextTMP;
    [SerializeField] private Text labelTextLegacy;

    [Header("Input References")]
    [SerializeField] private Button button;
    [SerializeField] private Toggle toggle;
    
    [Header("Animation Settings")]
    [SerializeField] private bool animated = false;
    [SerializeField] private float selectedScale = 1.1f;
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private GameObject selectedVisual;

    public string OptionValue { get; private set; }
    public bool IsSelected { get; private set; }

    public void Setup(string value, Action<string> onClick)
    {
        OptionValue = value;
        SetText(value);
        
        // Setup Button if exists
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(value));
        }
        
        // Setup Toggle if exists (for multi-select)
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) => onClick?.Invoke(value));
        }
        
        SetSelected(false, true);
    }

    public void SetText(string text)
    {
        if (labelTextTMP != null) labelTextTMP.text = text;
        if (labelTextLegacy != null) labelTextLegacy.text = text;
        
        // Fallback: search all children (including inactive) if references are missing
        if (labelTextTMP == null && labelTextLegacy == null)
        {
            var tmp = GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                labelTextTMP = tmp;
                tmp.text = text;
            }
            else
            {
                var legacy = GetComponentInChildren<Text>(true);
                if (legacy != null)
                {
                    labelTextLegacy = legacy;
                    legacy.text = text;
                }
            }
        }
    }

    public void SetSelected(bool selected, bool immediate = false)
    {
        IsSelected = selected;

        // Sync toggle state if using a toggle
        if (toggle != null && toggle.isOn != selected)
        {
            toggle.SetIsOnWithoutNotify(selected);
        }

        // Scale animation
        if (animated)
        {
            float targetScale = selected ? selectedScale : 1f;
            float duration = immediate ? 0 : animationDuration;

            transform.DOKill();
            transform.DOScale(targetScale, duration).SetEase(Ease.OutBack);
        }

        // Visual highlight
        if (selectedVisual != null)
        {
            selectedVisual.SetActive(selected);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
