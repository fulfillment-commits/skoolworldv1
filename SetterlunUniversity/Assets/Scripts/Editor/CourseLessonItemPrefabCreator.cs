using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SetterlunUniversity.Courses;

namespace SetterlunUniversity.Editor
{
    public static class CourseLessonItemPrefabCreator
    {
        private const string SavePath = "Assets/Prefabs/UI/CourseLessonItem.prefab";

        [MenuItem("Setterlun/Create Course Lesson Item Prefab")]
        public static void CreatePrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

            // ---------------------------------------------------------------
            // Root — row card for vertical list (fixed height, fills width)
            // ---------------------------------------------------------------
            var root = new GameObject("CourseLessonItem");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(0f, 64f);   // width controlled by layout group

            var rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.13f, 0.15f, 0.20f, 1f);

            var rootButton = root.AddComponent<Button>();
            var rootNav = rootButton.navigation;
            rootNav.mode = Navigation.Mode.None;
            rootButton.navigation = rootNav;

            // ---------------------------------------------------------------
            // Selected indicator — left blue bar
            // ---------------------------------------------------------------
            var selectedIndicator = CreateChild<Image>(root, "SelectedIndicator");
            selectedIndicator.color = new Color(0.13f, 0.55f, 0.88f, 1f);
            selectedIndicator.rectTransform.anchorMin = new Vector2(0f, 0f);
            selectedIndicator.rectTransform.anchorMax = new Vector2(0f, 1f);
            selectedIndicator.rectTransform.pivot     = new Vector2(0f, 0.5f);
            selectedIndicator.rectTransform.offsetMin = Vector2.zero;
            selectedIndicator.rectTransform.offsetMax = Vector2.zero;
            selectedIndicator.rectTransform.sizeDelta = new Vector2(4f, 0f);
            selectedIndicator.gameObject.SetActive(false);

            // ---------------------------------------------------------------
            // Lesson number badge — left
            // ---------------------------------------------------------------
            var numBg = CreateChild<Image>(root, "LessonNumberBg");
            numBg.color = new Color(0.20f, 0.22f, 0.30f, 1f);
            numBg.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            numBg.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            numBg.rectTransform.pivot     = new Vector2(0f, 0.5f);
            numBg.rectTransform.anchoredPosition = new Vector2(14f, 0f);
            numBg.rectTransform.sizeDelta = new Vector2(34f, 34f);

            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (knob != null) { numBg.sprite = knob; numBg.type = Image.Type.Sliced; }

            var lessonNumber = CreateChild<TextMeshProUGUI>(numBg.gameObject, "LessonNumber");
            lessonNumber.text = "01";
            lessonNumber.fontSize = 13f;
            lessonNumber.fontStyle = FontStyles.Bold;
            lessonNumber.alignment = TextAlignmentOptions.Center;
            lessonNumber.color = Color.white;
            FillParent(lessonNumber.rectTransform);

            // ---------------------------------------------------------------
            // Title — vertically centered in the row
            // ---------------------------------------------------------------
            var titleGo = CreateChild<TextMeshProUGUI>(root, "Title");
            titleGo.text = "Lesson Title";
            titleGo.fontSize = 13f;
            titleGo.fontStyle = FontStyles.Bold;
            titleGo.color = Color.white;
            titleGo.overflowMode = TextOverflowModes.Ellipsis;
            titleGo.enableWordWrapping = false;
            SetAnchors(titleGo.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(58f, 6f), new Vector2(-40f, -6f));

            // ---------------------------------------------------------------
            // Complete icon — right
            // ---------------------------------------------------------------
            var completeIcon = CreateChild<Image>(root, "CompleteIcon");
            completeIcon.color = new Color(0.75f, 0.75f, 0.75f, 1f);
            completeIcon.rectTransform.anchorMin = new Vector2(1f, 0.5f);
            completeIcon.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            completeIcon.rectTransform.pivot     = new Vector2(1f, 0.5f);
            completeIcon.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
            completeIcon.rectTransform.sizeDelta = new Vector2(22f, 22f);
            if (knob != null) completeIcon.sprite = knob;

            var tick = CreateChild<TextMeshProUGUI>(completeIcon.gameObject, "Tick");
            tick.text = "✓";
            tick.fontSize = 13f;
            tick.fontStyle = FontStyles.Bold;
            tick.alignment = TextAlignmentOptions.Center;
            tick.color = Color.white;
            FillParent(tick.rectTransform);

            // ---------------------------------------------------------------
            // Complete overlay — full card tint, hidden by default
            // ---------------------------------------------------------------
            var completeOverlay = CreateChild<Image>(root, "CompleteOverlay");
            completeOverlay.color = new Color(0.18f, 0.80f, 0.44f, 0.12f);
            FillParent(completeOverlay.rectTransform);
            completeOverlay.gameObject.SetActive(false);

            // ---------------------------------------------------------------
            // Wire CourseLessonItemUI
            // ---------------------------------------------------------------
            var itemUI = root.AddComponent<CourseLessonItemUI>();
            var so = new SerializedObject(itemUI);
            so.FindProperty("titleText").objectReferenceValue         = titleGo;
            so.FindProperty("lessonNumberText").objectReferenceValue  = lessonNumber;
            so.FindProperty("completeOverlay").objectReferenceValue   = completeOverlay.gameObject;
            so.FindProperty("completeIcon").objectReferenceValue      = completeIcon;
            so.FindProperty("selectedIndicator").objectReferenceValue = selectedIndicator.gameObject;
            so.FindProperty("itemButton").objectReferenceValue        = rootButton;
            so.FindProperty("completeIconColor").colorValue   = new Color(0.18f, 0.80f, 0.44f, 1f);
            so.FindProperty("incompleteIconColor").colorValue = new Color(0.75f, 0.75f, 0.75f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            var buttonSo = new SerializedObject(rootButton);
            buttonSo.FindProperty("m_TargetGraphic").objectReferenceValue = rootImage;
            buttonSo.ApplyModifiedPropertiesWithoutUndo();

            // ---------------------------------------------------------------
            // Save as prefab
            // ---------------------------------------------------------------
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, SavePath, out success);
            Object.DestroyImmediate(root);

            if (success)
            {
                Debug.Log($"[CourseLessonItemPrefabCreator] Prefab saved to {SavePath}");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SavePath);
                EditorGUIUtility.PingObject(prefab);
                Selection.activeObject = prefab;
            }
            else
            {
                Debug.LogError("[CourseLessonItemPrefabCreator] Failed to save prefab.");
            }
        }

        private static T CreateChild<T>(GameObject parent, string childName) where T : Component
        {
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go.AddComponent<T>();
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
    }
}
