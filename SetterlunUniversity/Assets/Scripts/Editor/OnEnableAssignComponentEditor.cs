using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OnEnableAssignComponent))]
public class OnEnableAssignComponentEditor : Editor
{
    private SerializedProperty formProperty;
    private SerializedProperty setFormProperty;

    private void OnEnable()
    {
        formProperty = serializedObject.FindProperty("form");
        setFormProperty = serializedObject.FindProperty("setForm");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        OnEnableAssignComponent component = (OnEnableAssignComponent)target;

        DrawHeader();
        DrawMainSettings(component);
        DrawButtons(component);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(6);

        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 15;
        titleStyle.alignment = TextAnchor.MiddleCenter;

        GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox);
        boxStyle.padding = new RectOffset(10, 10, 8, 8);

        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("Form Auto Size", titleStyle);

        EditorGUILayout.Space(2);

        EditorGUILayout.LabelField(
            "Resize a RectTransform automatically when this object becomes enabled.",
            EditorStyles.wordWrappedMiniLabel
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);
    }

    private void DrawMainSettings(OnEnableAssignComponent component)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(formProperty, new GUIContent("Form"));

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(setFormProperty, new GUIContent("Width / Height"));

        if (component.form != null)
        {
            Rect rect = component.form.rect;

            EditorGUILayout.Space(4);

            EditorGUILayout.LabelField(
                "Current Size",
                rect.width.ToString("0") + " x " + rect.height.ToString("0")
            );
        }
        else
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("Drag your RectTransform form here.", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawButtons(OnEnableAssignComponent component)
    {
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        GUI.enabled = component.GetComponent<RectTransform>() != null;

        if (GUILayout.Button("Use This RectTransform", GUILayout.Height(28)))
        {
            Undo.RecordObject(component, "Use This RectTransform");

            component.form = component.GetComponent<RectTransform>();

            EditorUtility.SetDirty(component);
        }

        GUI.enabled = component.form != null;

        if (GUILayout.Button("Copy Current Size", GUILayout.Height(28)))
        {
            Undo.RecordObject(component, "Copy Current Size");

            Rect rect = component.form.rect;
            component.setForm = new Vector2(rect.width, rect.height);

            EditorUtility.SetDirty(component);
        }

        if (GUILayout.Button("Apply Size Now", GUILayout.Height(32)))
        {
            Undo.RecordObject(component.form, "Apply Form Size");

            component.ApplySizeNow();

            EditorUtility.SetDirty(component.form);
            EditorUtility.SetDirty(component);
        }

        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }
}