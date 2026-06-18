using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreen : ScreenBase
{
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private float simulatedDuration = 2f;

    private float progress = 0f;
    private bool isLoading = false;
    private System.Action onLoadingComplete;

    public override void Show()
    {
        gameObject.SetActive(true);
        progress = 0f;
        isLoading = true;
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;
        if (statusText != null) statusText.text = "Loading...";
        OnShow();
    }

    public override void Hide()
    {
        isLoading = false;
        OnHide();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isLoading) return;

        progress += Time.deltaTime / simulatedDuration;
        if (progress >= 1f)
        {
            progress = 1f;
            isLoading = false;
            if (statusText != null) statusText.text = "Complete!";
            if (progressBarFill != null) progressBarFill.fillAmount = 1f;
            
            // Wait a small moment so the user can actually see the "Complete!" text
            StartCoroutine(DelayedCompleteRoutine());
        }

        if (progressBarFill != null && isLoading) progressBarFill.fillAmount = progress;
    }

    private IEnumerator DelayedCompleteRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        InvokeOnLoadingComplete();
    }

    public void StartLoading(System.Action onComplete, string status = "Loading...")
    {
        onLoadingComplete = onComplete;
        progress = 0f;
        isLoading = true;
        Show();
        if (statusText != null) statusText.text = status;
    }

    private void InvokeOnLoadingComplete()
    {
        var callback = onLoadingComplete;
        onLoadingComplete = null;
        callback?.Invoke();
    }

    public void SetStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        if (progressBarFill != null) progressBarFill.fillAmount = progress;
    }
}
