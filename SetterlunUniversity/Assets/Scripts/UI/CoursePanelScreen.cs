using System.Collections.Generic;
using ASAD_Multiplyer.Chat;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SetterlunUniversity.Courses;
using SetterlunUniversity.WebGL;

/// <summary>
/// Full-screen course panel shared by Business Model and Business Portal.
/// Assign a different CourseLessonData asset to each instance to create both panels.
/// </summary>
public class CoursePanelScreen : ScreenBase
{
    // ---------------------------------------------------------------
    // Inspector — Data
    // ---------------------------------------------------------------
    [Header("Course Data")]
    [Tooltip("Drag the CourseLessonData ScriptableObject for this panel (BusinessModel or BusinessPortal).")]
    [SerializeField] private CourseLessonData courseData;

    // ---------------------------------------------------------------
    // Inspector — Header
    // ---------------------------------------------------------------
    [Header("Header UI")]
    [SerializeField] private TMP_Text floorNameText;
    [SerializeField] private TMP_Text courseTitleText;
    [SerializeField] private Image headerBackground;
    [SerializeField] private Button closeButton;

    // ---------------------------------------------------------------
    // Inspector — Chapter / Lesson List
    // ---------------------------------------------------------------
    [Header("Lesson List (horizontal scroll)")]
    [SerializeField] private Transform lessonListContainer;     // Content child of ScrollRect
    [SerializeField] private CourseLessonItemUI lessonItemPrefab;
    [SerializeField] private ScrollRect lessonScrollRect;

    // ---------------------------------------------------------------
    // Inspector — Video / Iframe area
    // ---------------------------------------------------------------
    [Header("Iframe & Video Area")]
    [SerializeField] private IframeOverlayController iframeController;
    [SerializeField] private TMP_Text currentLessonDescriptionText;

    // ---------------------------------------------------------------
    // Inspector — Progress
    // ---------------------------------------------------------------
    // [Header("Progress")]
    // [SerializeField] private Slider progressSlider;
    // [SerializeField] private TMP_Text progressText;             // e.g. "3 / 8 Completed"

    [Header("Mark Complete Button")]
    [SerializeField] private Button markCompleteButton;
    [SerializeField] private TMP_Text markCompleteButtonText;
    [SerializeField] private Image markCompleteButtonImage;
    [SerializeField] private Color doneColor = new Color(0.18f, 0.80f, 0.44f);
    [SerializeField] private Color undoneColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Panel Switch Tabs")]
    [Tooltip("Button that switches to the Business Model panel.")]
    [SerializeField] private Button tabBizModelButton;
    [Tooltip("Button that switches to the Business Portal panel.")]
    [SerializeField] private Button tabBizPortalButton;
    [SerializeField] private Image tabBizModelBg;
    [SerializeField] private Image tabBizPortalBg;
    [SerializeField] private Color tabActiveColor   = new Color(1f, 1f, 1f, 0.22f);
    [SerializeField] private Color tabInactiveColor = new Color(1f, 1f, 1f, 0.06f);

    // ---------------------------------------------------------------
    // Runtime state
    // ---------------------------------------------------------------
    private readonly List<CourseLessonItemUI> _spawnedItems = new();
    private int _currentIndex = -1;

    // ---------------------------------------------------------------
    // ScreenBase overrides
    // ---------------------------------------------------------------
    protected override void OnShow()
    {
        if (courseData == null)
        {
            Debug.LogError("[CoursePanelScreen] courseData is not assigned.");
            return;
        }

        BuildList();
        RefreshHeader();
        RefreshTabs();

        // Select first uncompleted lesson, fall back to lesson 0
        int startIndex = 0;
        for (int i = 0; i < courseData.lessons.Count; i++)
        {
            if (!courseData.IsCompleted(i)) { startIndex = i; break; }
        }
        SelectLesson(startIndex, openIframe: true);

        PUN_ChatManager.Instance.showChatBtn = false;
    }

    protected override void OnHide()
    {
        iframeController?.HideIframe();
    }

    // ---------------------------------------------------------------
    // Unity lifecycle
    // ---------------------------------------------------------------
    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (markCompleteButton != null)
            markCompleteButton.onClick.AddListener(OnMarkCompleteClicked);

        if (iframeController != null)
            iframeController.OnVideoEnded.AddListener(OnVideoWatchedToEnd);

        if (tabBizModelButton != null)
            tabBizModelButton.onClick.AddListener(() =>
                ScreenManager.Instance?.ShowScreen(ScreenType.Course_BusinessModel));

        if (tabBizPortalButton != null)
            tabBizPortalButton.onClick.AddListener(() =>
                ScreenManager.Instance?.ShowScreen(ScreenType.Course_BusinessPortal));
    }

    // ---------------------------------------------------------------
    // List building
    // ---------------------------------------------------------------
    private void BuildList()
    {
        // Clear old items
        foreach (var item in _spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _spawnedItems.Clear();

        if (lessonItemPrefab == null || lessonListContainer == null) return;

        for (int i = 0; i < courseData.lessons.Count; i++)
        {
            CourseLessonItemUI item = Instantiate(lessonItemPrefab, lessonListContainer);
            item.Setup(this, i, courseData.lessons[i], courseData.IsCompleted(i));
            _spawnedItems.Add(item);
        }
    }

    // ---------------------------------------------------------------
    // Lesson selection — called by CourseLessonItemUI via OnLessonItemClicked
    // ---------------------------------------------------------------
    public void OnLessonItemClicked(int index)
    {
        SelectLesson(index, openIframe: true);
    }

    private void SelectLesson(int index, bool openIframe)
    {
        if (courseData == null || index < 0 || index >= courseData.lessons.Count) return;

        // Deselect old
        if (_currentIndex >= 0 && _currentIndex < _spawnedItems.Count)
            _spawnedItems[_currentIndex].SetSelected(false);

        _currentIndex = index;
        CourseLessonEntry entry = courseData.lessons[index];

        // Highlight new
        if (_currentIndex < _spawnedItems.Count)
            _spawnedItems[_currentIndex].SetSelected(true);

        // Update detail labels
        // if (currentLessonTitleText != null)
        //     currentLessonTitleText.text = entry.title;
        if (currentLessonDescriptionText != null)
            currentLessonDescriptionText.text = entry.description;

        RefreshMarkCompleteButton();
        ScrollToItem(index);

        if (openIframe && iframeController != null && !string.IsNullOrEmpty(entry.videoUrl))
            iframeController.ShowIframe(entry.videoUrl);
        else if (openIframe && iframeController != null)
            iframeController.HideIframe();
    }

    // ---------------------------------------------------------------
    // Mark complete
    // ---------------------------------------------------------------
    private void OnMarkCompleteClicked()
    {
        if (_currentIndex < 0) return;

        bool current = courseData.IsCompleted(_currentIndex);
        courseData.SetCompleted(_currentIndex, !current);

        if (_currentIndex < _spawnedItems.Count)
            _spawnedItems[_currentIndex].SetCompleted(!current);

        RefreshMarkCompleteButton();

        // Auto-advance to next uncompleted lesson
        if (!current)
        {
            int next = FindNextUncompleted(_currentIndex);
            if (next >= 0) SelectLesson(next, openIframe: true);
        }
    }

    private void OnVideoWatchedToEnd()
    {
        if (_currentIndex < 0 || courseData == null) return;
        if (courseData.IsCompleted(_currentIndex)) return;  // already done, skip

        courseData.SetCompleted(_currentIndex, true);

        if (_currentIndex < _spawnedItems.Count)
            _spawnedItems[_currentIndex].SetCompleted(true);

        RefreshMarkCompleteButton();

        int next = FindNextUncompleted(_currentIndex);
        if (next >= 0) SelectLesson(next, openIframe: true);
    }

    private int FindNextUncompleted(int after)
    {
        for (int i = after + 1; i < courseData.lessons.Count; i++)
            if (!courseData.IsCompleted(i)) return i;
        return -1;
    }

    private void RefreshMarkCompleteButton()
    {
        if (markCompleteButton == null || _currentIndex < 0) return;

        bool done = courseData.IsCompleted(_currentIndex);
        if (markCompleteButtonText != null)
            markCompleteButtonText.text = done ? "Mark Incomplete" : "Mark Complete";
        if (markCompleteButtonImage != null)
            markCompleteButtonImage.color = done ? doneColor : undoneColor;
    }

    // ---------------------------------------------------------------
    // Header / Progress
    // ---------------------------------------------------------------
    private void RefreshTabs()
    {
        bool isBizModel = (ScreenType == ScreenType.Course_BusinessModel);
        if (tabBizModelBg  != null) tabBizModelBg.color  = isBizModel  ? tabActiveColor : tabInactiveColor;
        if (tabBizPortalBg != null) tabBizPortalBg.color = !isBizModel ? tabActiveColor : tabInactiveColor;
    }

    private void RefreshHeader()
    {
        if (courseTitleText != null)       courseTitleText.text = courseData.courseName;
        // if (courseDescriptionText != null) courseDescriptionText.text = courseData.courseDescription;
        if (headerBackground != null)      headerBackground.color = courseData.accentColor;
    }

//     private void RefreshProgress()
//     {
//         if (courseData == null) return;
//         int total = courseData.lessons.Count;
//         int done = courseData.CompletedCount();
// }

    // ---------------------------------------------------------------
    // Scroll chapter list so selected item is centred
    // ---------------------------------------------------------------
    private void ScrollToItem(int index)
    {
        if (lessonScrollRect == null || _spawnedItems.Count == 0) return;
        if (index < 0 || index >= _spawnedItems.Count) return;

        // Vertical list: top = 1.0, bottom = 0.0
        float t = 1f - (float)index / Mathf.Max(1, _spawnedItems.Count - 1);
        lessonScrollRect.verticalNormalizedPosition = t;
    }

    // ---------------------------------------------------------------
    // Close
    // ---------------------------------------------------------------
    private void OnCloseClicked()
    {
        iframeController?.HideIframe();
        PUN_ChatManager.Instance.showChatBtn = true;
        if (ScreenManager.Instance != null)
            ScreenManager.Instance.GoBack();
        else
            Hide();
    }

    // ---------------------------------------------------------------
    // Public helpers — wired from external buttons (Business Model / Portal buttons in world)
    // ---------------------------------------------------------------

    /// <summary>Open this panel and immediately load a specific lesson URL.</summary>
    public void OpenWithUrl(string url)
    {
        Show();
        if (iframeController != null)
            iframeController.ShowIframe(url);
    }

    /// <summary>Open this panel at a specific lesson index.</summary>
    public void OpenAtLesson(int index)
    {
        Show();
        SelectLesson(index, openIframe: true);
    }
}
