using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class DynamicMessagePanel : MonoBehaviour
{
    public static DynamicMessagePanel Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float slideInOffset = 100f;
    [SerializeField] private float targetY = 30f;
    [SerializeField] private bool useFixedTargetY = true;
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Vector2 originalAnchoredPosition;
    private RectTransform messagePanelRect;
    private float finalTargetY;
    private Action onCloseCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (messagePanel != null)
        {
            messagePanelRect = messagePanel.GetComponent<RectTransform>();
            if (messagePanelRect != null)
            {
                originalAnchoredPosition = messagePanelRect.anchoredPosition;
                finalTargetY = useFixedTargetY ? targetY : originalAnchoredPosition.y;
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        if (canvasGroup == null && messagePanel != null)
        {
            canvasGroup = messagePanel.GetComponent<CanvasGroup>();
        }

        gameObject.SetActive(false);
    }

    private void OnCloseButtonClicked()
    {
        Hide();
    }

    public void ShowMessage(string message, Action onClose = null)
    {
        if (messagePanel == null || messageText == null)
        {
            Debug.LogError("DynamicMessagePanel: Message Panel or Message Text is not assigned!");
            return;
        }

        onCloseCallback = onClose;
        messageText.text = message;
        gameObject.SetActive(true);

        if (ScreenManager.Instance != null)
        {
            ScreenManager.Instance.UpdatePlayerInputState();
        }

        if (messagePanelRect != null)
        {
            canvasGroup.alpha = 0f;
            messagePanelRect.anchoredPosition = new Vector2(originalAnchoredPosition.x, finalTargetY + slideInOffset);

            canvasGroup.DOFade(1f, fadeInDuration).SetEase(easeType);
            messagePanelRect.DOAnchorPosY(finalTargetY, fadeInDuration).SetEase(easeType);
        }
    }

    public void ShowMessage(string message, string closeButtonText, Action onClose = null)
    {
        if (closeButton != null)
        {
            var buttonText = closeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = closeButtonText;
            }
        }
        ShowMessage(message, onClose);
    }

    /// <summary>
    /// Helper wrapper for Onboarding sequence
    /// </summary>
    public void Show(string message, Action onClose)
    {
        ShowMessage(message, "Close", onClose);
    }

    public void Hide()
    {
        if (gameObject == null || !gameObject.activeSelf) return;

        if (messagePanelRect != null)
        {
            canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                gameObject.SetActive(false);
                if (ScreenManager.Instance != null)
                {
                    ScreenManager.Instance.UpdatePlayerInputState();
                }
                messagePanelRect.anchoredPosition = new Vector2(originalAnchoredPosition.x, finalTargetY);
                canvasGroup.alpha = 1f;
                onCloseCallback?.Invoke();
            });

            messagePanelRect.DOAnchorPosY(finalTargetY + slideInOffset, fadeOutDuration).SetEase(Ease.InBack);
        }
    }

    public void HideImmediate()
    {
        if (gameObject != null)
        {
            gameObject.SetActive(false);
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.UpdatePlayerInputState();
            }
            if (messagePanelRect != null)
            {
                messagePanelRect.anchoredPosition = new Vector2(originalAnchoredPosition.x, finalTargetY);
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
    }
}
