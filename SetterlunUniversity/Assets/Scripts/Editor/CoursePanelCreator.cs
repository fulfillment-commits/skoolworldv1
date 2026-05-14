using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SetterlunUniversity.Courses;
using SetterlunUniversity.WebGL;
using Unity.VisualScripting;

namespace SetterlunUniversity.Editor
{
    public static class CoursePanelCreator
    {
        // ---------------------------------------------------------------
        // Test buttons
        // ---------------------------------------------------------------
        [MenuItem("Setterlun/Add Test Buttons to Canvas")]
        public static void AddTestButtons()
        {
            Canvas canvas = FindSceneCanvas();
            if (canvas == null)
            {
                Debug.LogError("[CoursePanelCreator] No Canvas found in scene.");
                return;
            }

            var container = new GameObject("CourseTestButtons");
            container.transform.SetParent(canvas.transform, false);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0f);
            containerRect.anchorMax = new Vector2(0f, 0f);
            containerRect.pivot     = new Vector2(0f, 0f);
            containerRect.anchoredPosition = new Vector2(20f, 20f);
            containerRect.sizeDelta = new Vector2(220f, 110f);

            var vlg = container.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            MakeTestButton(container, "Test: Business Model",
                new Color(0.13f, 0.55f, 0.88f, 1f), ScreenType.Course_BusinessModel);
            MakeTestButton(container, "Test: Business Portal",
                new Color(0.20f, 0.72f, 0.52f, 1f), ScreenType.Course_BusinessPortal);

            Undo.RegisterCreatedObjectUndo(container, "Add Course Test Buttons");
            Selection.activeGameObject = container;
            Debug.Log("[CoursePanelCreator] Test buttons added to Canvas bottom-left corner.");
        }

        private static void MakeTestButton(GameObject parent, string label, Color color, ScreenType target)
        {
            var go = new GameObject(label.Replace(":", "").Replace(" ", ""));
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 44f);

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;

            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            var helper = go.AddComponent<CourseTestButtonHelper>();
            var helperSO = new SerializedObject(helper);
            helperSO.FindProperty("targetScreen").enumValueIndex = (int)target;
            helperSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, helper.Open);

            var tmp = MakeTMP("Label", go, 13f, FontStyles.Bold);
            tmp.text = label;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            FillParent(tmp.rectTransform);
        }

        // ---------------------------------------------------------------
        // Menu items
        // ---------------------------------------------------------------
        [MenuItem("Setterlun/Create Course Panel/Business Model Panel")]
        public static void CreateBusinessModelPanel()
        {
            var data = AssetDatabase.LoadAssetAtPath<CourseLessonData>(
                "Assets/Setterlun_University/BusinessModelCourse.asset");
            var lessonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/CourseLessonItem.prefab");

            CreatePanel("CoursePanelScreen_BusinessModel",
                ScreenType.Course_BusinessModel, data, lessonPrefab,
                new Color(0.13f, 0.55f, 0.88f, 1f));
        }

        [MenuItem("Setterlun/Create Course Panel/Business Portal Panel")]
        public static void CreateBusinessPortalPanel()
        {
            var data = AssetDatabase.LoadAssetAtPath<CourseLessonData>(
                "Assets/Setterlun_University/BusinessPortalCourse.asset");
            var lessonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/UI/CourseLessonItem.prefab");

            CreatePanel("CoursePanelScreen_BusinessPortal",
                ScreenType.Course_BusinessPortal, data, lessonPrefab,
                new Color(0.20f, 0.72f, 0.52f, 1f));
        }

        // ---------------------------------------------------------------
        // Panel builder
        // ---------------------------------------------------------------
        private static void CreatePanel(string panelName, ScreenType screenType,
            CourseLessonData data, GameObject lessonPrefab, Color accent)
        {
            Canvas canvas = FindSceneCanvas();
            Transform canvasTransform = canvas != null ? canvas.transform : null;

            if (lessonPrefab == null)
                Debug.LogWarning("[CoursePanelCreator] CourseLessonItem prefab not found at " +
                    "'Assets/Prefabs/UI/CourseLessonItem.prefab'. " +
                    "Run 'Setterlun/Create Course Lesson Item Prefab' first, then re-run this menu item.");

            // ---------------------------------------------------------------
            // Root panel
            // ---------------------------------------------------------------
            var root = new GameObject(panelName);
            if (canvasTransform != null) root.transform.SetParent(canvasTransform, false);

            var rootRect = root.AddComponent<RectTransform>();
            FillParent(rootRect);

            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.07f, 0.09f, 0.13f, 0.97f);

            var screen = root.AddComponent<CoursePanelScreen>();

            var screenSO = new SerializedObject(screen);
            var screenTypeProp = screenSO.FindProperty("<ScreenType>k__BackingField");
            if (screenTypeProp != null)
                screenTypeProp.enumValueIndex = (int)screenType;
            else
                Debug.LogWarning("[CoursePanelCreator] Could not find ScreenType property — set it manually.");
            screenSO.ApplyModifiedPropertiesWithoutUndo();

            // ---------------------------------------------------------------
            // Header bar — top 12 %
            // ---------------------------------------------------------------
            var header = MakeRect("Header", root);
            var headerImg = header.AddComponent<Image>();
            headerImg.color = accent;
            SetAnchors(header, new Vector2(0f, 0.88f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);

            var titleTMP = MakeTMP("CourseTitleText", header.gameObject, 20f, FontStyles.Bold);
            titleTMP.text = data != null ? data.courseName : "Course Title";
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(titleTMP.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0.50f, 1f),
                new Vector2(16f, 0f), new Vector2(-8f, 0f));

            var descTMP = MakeTMP("CourseDescriptionText", header.gameObject, 11f, FontStyles.Normal);
            descTMP.text = data != null ? data.courseDescription : "";
            descTMP.color = new Color(1f, 1f, 1f, 0.75f);
            descTMP.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(descTMP.rectTransform,
                new Vector2(0f, 0f), new Vector2(0.50f, 0.5f),
                new Vector2(16f, 0f), new Vector2(-8f, 0f));

            bool isModelPanel = (screenType == ScreenType.Course_BusinessModel);

            var tabBizModel = MakeTabButton("Tab_BusinessModel", header.gameObject,
                "Business Model",
                new Vector2(0.50f, 0f), new Vector2(0.70f, 1f),
                new Vector2(4f, 6f), new Vector2(-4f, -6f),
                isModelPanel ? new Color(1f,1f,1f,0.22f) : new Color(1f,1f,1f,0.06f));

            var tabBizPortal = MakeTabButton("Tab_BusinessPortal", header.gameObject,
                "Business Portal",
                new Vector2(0.70f, 0f), new Vector2(0.88f, 1f),
                new Vector2(4f, 6f), new Vector2(-4f, -6f),
                !isModelPanel ? new Color(1f,1f,1f,0.22f) : new Color(1f,1f,1f,0.06f));

            var closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(header.gameObject.transform, false);
            var closeBtnRect = closeBtnGo.AddComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(0.88f, 0.15f);
            closeBtnRect.anchorMax = new Vector2(1f, 0.85f);
            closeBtnRect.offsetMin = new Vector2(4f, 0f);
            closeBtnRect.offsetMax = new Vector2(-12f, 0f);
            closeBtnGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
            var closeBtn = closeBtnGo.AddComponent<Button>();
            var closeTMP = MakeTMP("Label", closeBtnGo, 20f, FontStyles.Bold);
            closeTMP.text = "✕";
            closeTMP.color = Color.white;
            closeTMP.alignment = TextAlignmentOptions.Center;
            FillParent(closeTMP.rectTransform);

            // ---------------------------------------------------------------
            // Progress bar — row 82 %–88 % full width
            // ---------------------------------------------------------------
            var progressArea = MakeRect("ProgressArea", root);
            progressArea.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.17f, 1f);
            SetAnchors(progressArea, new Vector2(0f, 0.82f), new Vector2(1f, 0.88f),
                Vector2.zero, Vector2.zero);

            var progressSlider = CreateSlider(progressArea.gameObject, accent);
            SetAnchors(progressSlider.GetComponent<RectTransform>(),
                new Vector2(0f, 0f), new Vector2(0.78f, 1f),
                new Vector2(12f, 6f), new Vector2(-8f, -6f));

            var progressTMP = MakeTMP("ProgressText", progressArea.gameObject, 11f, FontStyles.Normal);
            progressTMP.text = "0 / 0 Completed";
            progressTMP.color = new Color(1f, 1f, 1f, 0.7f);
            progressTMP.alignment = TextAlignmentOptions.MidlineRight;
            SetAnchors(progressTMP.rectTransform,
                new Vector2(0.78f, 0f), new Vector2(1f, 1f),
                new Vector2(4f, 0f), new Vector2(-12f, 0f));

            // ---------------------------------------------------------------
            // Video area — LEFT column top  (0–65 %, row 30 %–82 %)
            // ---------------------------------------------------------------
            var videoArea = MakeRect("VideoArea", root);
            videoArea.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            SetAnchors(videoArea,
                new Vector2(0f,    0.30f), new Vector2(0.65f, 0.82f),
                new Vector2(12f,   4f),    new Vector2(-6f,   -4f));

            var iframePlaceholder = MakeRect("IframePlaceholder", videoArea.gameObject);
            iframePlaceholder.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);
            FillParent(iframePlaceholder);

            var iframeCtrl = iframePlaceholder.gameObject.AddComponent<IframeOverlayController>();
            var iframeSO = new SerializedObject(iframeCtrl);
            iframeSO.FindProperty("iframeId").stringValue                     = panelName + "_iframe";
            iframeSO.FindProperty("iframeUrl").stringValue                    = "";
            iframeSO.FindProperty("targetRectTransform").objectReferenceValue = iframePlaceholder;
            iframeSO.FindProperty("showOnStart").boolValue                    = false;
            iframeSO.FindProperty("syncEveryFrameWhileVisible").boolValue     = true;
            iframeSO.FindProperty("showCloseButton").boolValue                = false;
            iframeSO.FindProperty("allowAutoplay").boolValue                  = true;
            iframeSO.FindProperty("allowFullscreen").boolValue                = true;
            iframeSO.FindProperty("zIndex").intValue                          = 9999;
            iframeSO.ApplyModifiedPropertiesWithoutUndo();

            // ---------------------------------------------------------------
            // Lesson detail panel — LEFT column bottom  (0–65 %, row 0–30 %)
            // Shows selected lesson title, description and Mark Complete button
            // ---------------------------------------------------------------
            var detailPanel = MakeRect("LessonDetailPanel", root);
            detailPanel.AddComponent<Image>().color = new Color(0.10f, 0.12f, 0.17f, 1f);
            SetAnchors(detailPanel,
                new Vector2(0f,    0f),    new Vector2(0.65f, 0.30f),
                new Vector2(12f,   4f),    new Vector2(-6f,   -4f));

            var lessonTitleTMP = MakeTMP("CurrentLessonTitle", detailPanel.gameObject, 14f, FontStyles.Bold);
            lessonTitleTMP.text = "Select a Lesson";
            lessonTitleTMP.color = Color.white;
            lessonTitleTMP.alignment = TextAlignmentOptions.TopLeft;
            lessonTitleTMP.overflowMode = TextOverflowModes.Ellipsis;
            lessonTitleTMP.enableWordWrapping = true;
            SetAnchors(lessonTitleTMP.rectTransform,
                new Vector2(0f, 0.55f), new Vector2(1f, 1f),
                new Vector2(12f, 6f), new Vector2(-12f, -4f));

            var lessonDescTMP = MakeTMP("CurrentLessonDescription", detailPanel.gameObject, 11f, FontStyles.Normal);
            lessonDescTMP.text = "";
            lessonDescTMP.color = new Color(1f, 1f, 1f, 0.68f);
            lessonDescTMP.alignment = TextAlignmentOptions.TopLeft;
            lessonDescTMP.overflowMode = TextOverflowModes.Ellipsis;
            lessonDescTMP.enableWordWrapping = true;
            SetAnchors(lessonDescTMP.rectTransform,
                new Vector2(0f, 0.25f), new Vector2(1f, 0.55f),
                new Vector2(12f, 4f), new Vector2(-12f, -4f));

            var markBtnGo = new GameObject("MarkCompleteButton");
            markBtnGo.transform.SetParent(detailPanel.gameObject.transform, false);
            var markBtnRect = markBtnGo.AddComponent<RectTransform>();
            markBtnRect.anchorMin = new Vector2(0f, 0f);
            markBtnRect.anchorMax = new Vector2(1f, 0.23f);
            markBtnRect.offsetMin = new Vector2(12f, 8f);
            markBtnRect.offsetMax = new Vector2(-12f, -4f);
            var markBtnImg = markBtnGo.AddComponent<Image>();
            markBtnImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var markBtn = markBtnGo.AddComponent<Button>();
            var markBtnNav = markBtn.navigation;
            markBtnNav.mode = Navigation.Mode.None;
            markBtn.navigation = markBtnNav;

            var markBtnTMP = MakeTMP("Label", markBtnGo, 13f, FontStyles.Bold);
            markBtnTMP.text = "Mark Complete";
            markBtnTMP.color = Color.white;
            markBtnTMP.alignment = TextAlignmentOptions.Center;
            FillParent(markBtnTMP.rectTransform);

            // ---------------------------------------------------------------
            // Vertical lesson list — RIGHT column  (65–100 %, row 0–82 %)
            // ---------------------------------------------------------------
            var scrollRoot = MakeRect("ChapterScrollRect", root);
            scrollRoot.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.15f, 1f);
            SetAnchors(scrollRoot,
                new Vector2(0.65f, 0f), new Vector2(1f,   0.82f),
                new Vector2(2f,    0f), new Vector2(0f,   -2f));

            var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal       = false;
            scrollRect.vertical         = true;
            scrollRect.movementType     = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.decelerationRate = 0.135f;

            var viewport = MakeRect("Viewport", scrollRoot.gameObject);
            FillParent(viewport);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            scrollRect.viewport = viewport;

            var content = MakeRect("Content", viewport.gameObject);
            // Top-anchored so items stack downward
            content.anchorMin         = new Vector2(0f,   1f);
            content.anchorMax         = new Vector2(1f,   1f);
            content.pivot             = new Vector2(0.5f, 1f);
            content.anchoredPosition  = Vector2.zero;
            content.sizeDelta         = Vector2.zero;

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing              = 4f;
            vlg.padding              = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth    = true;
            vlg.childControlHeight   = false;   // items have fixed height set by prefab
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = content;

            // ---------------------------------------------------------------
            // Wire CoursePanelScreen serialized fields
            // ---------------------------------------------------------------
            CourseLessonItemUI lessonItemUI = lessonPrefab != null
                ? lessonPrefab.GetComponent<CourseLessonItemUI>() : null;
            if (lessonPrefab != null && lessonItemUI == null)
                Debug.LogWarning("[CoursePanelCreator] CourseLessonItem prefab has no CourseLessonItemUI on its root.");

            var panelSO = new SerializedObject(screen);
            panelSO.FindProperty("courseData").objectReferenceValue                 = data;
            panelSO.FindProperty("courseTitleText").objectReferenceValue            = titleTMP;
            panelSO.FindProperty("courseDescriptionText").objectReferenceValue      = descTMP;
            panelSO.FindProperty("headerBackground").objectReferenceValue           = headerImg;
            panelSO.FindProperty("closeButton").objectReferenceValue                = closeBtn;
            panelSO.FindProperty("lessonListContainer").objectReferenceValue        = content;
            panelSO.FindProperty("lessonItemPrefab").objectReferenceValue           = lessonItemUI;
            panelSO.FindProperty("lessonScrollRect").objectReferenceValue           = scrollRect;
            panelSO.FindProperty("iframeController").objectReferenceValue           = iframeCtrl;
            panelSO.FindProperty("currentLessonTitleText").objectReferenceValue     = lessonTitleTMP;
            panelSO.FindProperty("currentLessonDescriptionText").objectReferenceValue = lessonDescTMP;
            panelSO.FindProperty("progressSlider").objectReferenceValue             = progressSlider;
            panelSO.FindProperty("progressText").objectReferenceValue               = progressTMP;
            panelSO.FindProperty("markCompleteButton").objectReferenceValue         = markBtn;
            panelSO.FindProperty("markCompleteButtonText").objectReferenceValue     = markBtnTMP;
            panelSO.FindProperty("markCompleteButtonImage").objectReferenceValue    = markBtnImg;
            panelSO.FindProperty("tabBizModelButton").objectReferenceValue          = tabBizModel.btn;
            panelSO.FindProperty("tabBizPortalButton").objectReferenceValue         = tabBizPortal.btn;
            panelSO.FindProperty("tabBizModelBg").objectReferenceValue              = tabBizModel.bg;
            panelSO.FindProperty("tabBizPortalBg").objectReferenceValue             = tabBizPortal.bg;
            panelSO.ApplyModifiedPropertiesWithoutUndo();

            if (canvas != null)
            {
                iframeSO = new SerializedObject(iframeCtrl);
                iframeSO.FindProperty("targetCanvas").objectReferenceValue = canvas;
                iframeSO.ApplyModifiedPropertiesWithoutUndo();
            }

            root.SetActive(false);

            Undo.RegisterCreatedObjectUndo(root, $"Create {panelName}");
            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);

            string warn = lessonItemUI == null
                ? " WARNING: lessonItemPrefab is null — run 'Setterlun/Create Course Lesson Item Prefab' first!"
                : "";
            Debug.Log($"[CoursePanelCreator] '{panelName}' created.{warn} " +
                      "Add it to ScreenManager's All Screens array to finish setup.");
        }

        // ---------------------------------------------------------------
        // Slider factory
        // ---------------------------------------------------------------
        private static Slider CreateSlider(GameObject parent, Color fillColor)
        {
            var sliderGo = new GameObject("ProgressSlider");
            sliderGo.transform.SetParent(parent.transform, false);
            sliderGo.AddComponent<RectTransform>();
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue    = 0f;
            slider.maxValue    = 1f;
            slider.value       = 0f;
            slider.wholeNumbers = false;
            slider.interactable = false;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillGo.AddComponent<Image>().color = fillColor;

            slider.fillRect = fillRect;
            var nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;

            return slider;
        }

        // ---------------------------------------------------------------
        // Canvas finder — always prefer ScreenManager's child canvas
        // ---------------------------------------------------------------
        private static Canvas FindSceneCanvas()
        {
            var screenManager = Object.FindObjectOfType<ScreenManager>();
            if (screenManager != null)
            {
                var canvas = screenManager.GetComponentInChildren<Canvas>(true);
                if (canvas != null) return canvas;

                canvas = screenManager.GetComponent<Canvas>();
                if (canvas != null) return canvas;
            }

            var allCanvases = Object.FindObjectsOfType<Canvas>(true);
            foreach (var c in allCanvases)
            {
                string n = c.gameObject.name.ToLower();
                if (n.Contains("screen") || (n.Contains("ui") && !n.Contains("gameplay")))
                    return c;
            }

            return allCanvases.Length > 0 ? allCanvases[0] : null;
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------
        private static RectTransform MakeRect(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<RectTransform>();
        }

        private static RectTransform MakeRect(string name, RectTransform parent)
            => MakeRect(name, parent.gameObject);

        private static TextMeshProUGUI MakeTMP(string name, GameObject parent,
            float fontSize, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            return tmp;
        }

        private static void SetAnchors(RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private struct TabResult { public Button btn; public Image bg; }

        private static TabResult MakeTabButton(string name, GameObject parent, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var bg = go.AddComponent<Image>();
            bg.color = bgColor;

            var btn = go.AddComponent<Button>();
            var nav = btn.navigation;
            nav.mode = Navigation.Mode.None;
            btn.navigation = nav;

            var tmp = MakeTMP("Label", go, 11f, FontStyles.Bold);
            tmp.text = label;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            FillParent(tmp.rectTransform);

            return new TabResult { btn = btn, bg = bg };
        }
    }
}
