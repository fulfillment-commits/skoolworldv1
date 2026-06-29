using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float sceneReadyDelay = 0.5f;
    [SerializeField] private int setupFramesAfterSceneLoad = 2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SceneTransitionManager] Duplicate instance found, destroying component.");
            Destroy(this);
            return;
        }

        Instance = this;
        
        // If this component is on a shared object (like ScreenManager), 
        // DontDestroyOnLoad might already be called, but calling it again is safe.
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (fadeCanvasGroup == null)
        {
            CreateFadeUI();
        }
        else
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void CreateFadeUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Always on top
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create Fade Image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        // Add CanvasGroup for fading
        fadeCanvasGroup = imageObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }

    private string targetSpawnPoint;

    public void TransitionToScene(string sceneName, string spawnPointName = null, bool useFade = true)
    {
        targetSpawnPoint = spawnPointName;
        StartCoroutine(TransitionRoutine(sceneName, useFade));
    }

    public string GetTargetSpawnPoint()
    {
        string spawn = targetSpawnPoint;
        targetSpawnPoint = null; // Clear after use
        return spawn;
    }

    private IEnumerator TransitionRoutine(string sceneName, bool useFade)
    {
        if (fadeCanvasGroup == null && useFade)
        {
            Debug.LogError("[SceneTransitionManager] FadeCanvasGroup is not assigned!");
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        // Fade Out (Only if requested)
        if (useFade && fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(Fade(0, 1));
        }

        // Load Scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneTransitionManager] Scene '{sceneName}' could not be loaded. Please check if it's added to Build Settings.");
            if (fadeCanvasGroup != null) fadeCanvasGroup.gameObject.SetActive(false);
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            UpdateLoadingProgress(asyncLoad.progress);
            yield return null;
        }

        yield return StartCoroutine(WaitForSceneReady(sceneName));

        // Fade In (Only if requested)
        if (useFade && fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1, 0));
            fadeCanvasGroup.gameObject.SetActive(false);
        }
        else if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        // 4. Hide Loading Screen and Show World UI after transition is fully complete
        if (ScreenManager.Instance != null)
        {
            var loading = ScreenManager.Instance.GetLoadingScreen();
            if (loading != null) loading.Hide();

            bool allQuestsCompleted = OnboardingQuestManager.Instance != null && OnboardingQuestManager.Instance.GetCompletedQuestCount() >= 10;
            ScreenManager.Instance.ShowScreen(ScreenType.MainWorld);

            // Also auto-show the specific quest screen only while onboarding is still in progress.
            if (!allQuestsCompleted)
            {
                if (OnboardingManager.Instance != null)
                {
                    OnboardingManager.Instance.ShowNextIncompleteQuest();
                }
            }
        }
    }

    private IEnumerator WaitForSceneReady(string sceneName)
    {
        while (SceneManager.GetActiveScene().name != sceneName || !SceneManager.GetActiveScene().isLoaded)
        {
            yield return null;
        }

        for (int i = 0; i < setupFramesAfterSceneLoad; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        if (sceneReadyDelay > 0f)
        {
            yield return new WaitForSeconds(sceneReadyDelay);
        }

        UpdateLoadingProgress(1f);
    }

    private static void UpdateLoadingProgress(float progress)
    {
        if (ScreenManager.Instance == null)
        {
            return;
        }

        LoadingScreen loading = ScreenManager.Instance.GetLoadingScreen();
        if (loading != null)
        {
            loading.SetProgress(Mathf.Clamp01(progress));
        }
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = endAlpha;
    }
}
