using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GameSettings settings = (GameSettings)target;

        serializedObject.Update();

        EditorGUILayout.LabelField("Network Settings", EditorStyles.boldLabel);
        
        SerializedProperty isLocalProp = serializedObject.FindProperty("isLocal");
        EditorGUILayout.PropertyField(isLocalProp);

        EditorGUI.BeginDisabledGroup(!settings.IsLocal);
        SerializedProperty localUrlProp = serializedObject.FindProperty("localUrl");
        EditorGUILayout.PropertyField(localUrlProp);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(settings.IsLocal);
        SerializedProperty domainUrlProp = serializedObject.FindProperty("domainUrl");
        EditorGUILayout.PropertyField(domainUrlProp);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Demo Mode", EditorStyles.boldLabel);
        SerializedProperty demoModeProp = serializedObject.FindProperty("useLocalDemoMode");
        EditorGUILayout.PropertyField(demoModeProp);

        serializedObject.ApplyModifiedProperties();
    }
}
