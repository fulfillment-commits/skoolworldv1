using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class SceneChanger : EditorWindow {

    private Vector2 scrollPosition;
    private int draggedSceneIndex = -1;
    private bool isDragging = false;
    
    // Cache the scenes array to avoid constant rebuilding
    private EditorBuildSettingsScene[] cachedScenes;
    private bool needsRefresh = true;

    [MenuItem("Asad/Scene Changer")]
    static void Init() {
        SceneChanger window = (SceneChanger)EditorWindow.GetWindow(typeof(SceneChanger));
        window.Show();
    }

    void OnEnable() {
        RefreshScenesList();
    }

    void OnFocus() {
        RefreshScenesList();
    }

    void RefreshScenesList() {
        cachedScenes = EditorBuildSettings.scenes;
        needsRefresh = false;
    }

    void OnGUI() {
        EditorGUILayout.LabelField("Asad Scene Changer", EditorStyles.boldLabel, GUILayout.ExpandWidth(true), GUILayout.Height(40));

        if (EditorApplication.isCompiling) {
            EditorGUILayout.HelpBox("Compiling Scripts", MessageType.Info);
            return;
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorGUILayout.HelpBox("Play Mode Active", MessageType.Warning);
            return;
        }
        if (EditorApplication.isUpdating) {
            EditorGUILayout.HelpBox("Updating AssetDatabase", MessageType.Info);
            return;
        }

        // Refresh if needed
        if (needsRefresh || cachedScenes == null) {
            RefreshScenesList();
        }

        // Handle drag and drop from anywhere in the window
        HandleGlobalDragAndDrop();

        HandleDragAndDropArea();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Track changes that need to be applied
        bool scenesChanged = false;
        int sceneToRemove = -1;
        List<EditorBuildSettingsScene> modifiedScenes = new List<EditorBuildSettingsScene>(cachedScenes);

        for (int i = 0; i < cachedScenes.Length; i++) {
            bool isOpenScene = (cachedScenes[i].path == UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            
            string[] pathParts = cachedScenes[i].path.Split('/');
            string sceneFileName = pathParts[pathParts.Length - 1];
            string sceneName = sceneFileName.Split('.')[0];

            GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox);
            GUIStyle textStyle = new GUIStyle((isOpenScene) ? EditorStyles.boldLabel : EditorStyles.label);
            textStyle.fontSize = (isOpenScene) ? 14 : 12;

            if (isOpenScene) {
                boxStyle.normal.background = MakeColorTexture(1, 1, new Color(0.0f, 0.75f, 1.0f, 0.5f));
            }
            
            if (isDragging && draggedSceneIndex == i) {
                boxStyle.normal.background = MakeColorTexture(1, 1, new Color(1, 1, 0, 0.3f));
            }
            
            Rect sceneRect = EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.BeginHorizontal();

            // Handle drag only on the ≡ handle
            Rect handleRect = GUILayoutUtility.GetRect(15, EditorGUIUtility.singleLineHeight, GUILayout.Width(15));
            GUI.Label(handleRect, "≡", EditorStyles.boldLabel);
            HandleSceneDragAndDrop(i, handleRect);
            
            GUILayout.Label($"{i}", EditorStyles.miniLabel, GUILayout.Width(20));

            // Enable/disable toggle for build settings
            bool currentEnabled = cachedScenes[i].enabled;
            bool newEnabled = EditorGUILayout.Toggle(currentEnabled, GUILayout.Width(20));
            
            if (newEnabled != currentEnabled) {
                modifiedScenes[i] = new EditorBuildSettingsScene(cachedScenes[i].path, newEnabled);
                scenesChanged = true;
            }
            
            // Open/Save scene button
            if (GUILayout.Button(isOpenScene ? "Save" : "Open", GUILayout.Width(50))) {
                if (isOpenScene) {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                } else {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(cachedScenes[i].path);
                }
            }
            
            // Ping scene asset in project window
            if (GUILayout.Button("Ping", GUILayout.Width(50))) {
                UnityEngine.Object sceneAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(cachedScenes[i].path);
                if (sceneAsset != null) {
                    EditorGUIUtility.PingObject(sceneAsset);
                }
            }

            EditorGUILayout.LabelField(sceneName, textStyle);

            // Copy scene name to clipboard
            if (GUILayout.Button("Copy", GUILayout.Width(50))) {
                EditorGUIUtility.systemCopyBuffer = sceneName;
            }
            
            // Remove scene from build settings
            if (GUILayout.Button("X", GUILayout.Width(25))) {
                sceneToRemove = i;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
        
        // Apply scene changes after the GUI loop to avoid conflicts
        if (scenesChanged) {
            EditorBuildSettings.scenes = modifiedScenes.ToArray();
            cachedScenes = modifiedScenes.ToArray();
        }
        
        // Remove scene if requested
        if (sceneToRemove >= 0) {
            RemoveSceneFromBuild(sceneToRemove);
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal("Box");
        if (GUILayout.Button("New Scene")) {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);
        }
        if (GUILayout.Button("Save Scene")) {
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        if (EditorApplication.isRemoteConnected) {
            EditorGUILayout.HelpBox("Unity Remote Connected", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("💡 Tips:\n• Drag scene files to drop area to add to build\n• Drag scenes by the ≡ handle to reorder build index\n• Use 'X' button to remove scenes\n• Use 'Refresh List' if changes don't appear", MessageType.Info);
    }
    
    void HandleGlobalDragAndDrop()
    {
        Event evt = Event.current;
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
                // Check if we have any scene assets being dragged
                bool hasSceneAssets = false;
                if (DragAndDrop.objectReferences != null)
                {
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is SceneAsset)
                        {
                            hasSceneAssets = true;
                            break;
                        }
                    }
                }
                
                if (hasSceneAssets)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                }
                break;
                
            case EventType.DragPerform:
                bool addedScenes = false;
                if (DragAndDrop.objectReferences != null)
                {
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        if (obj is SceneAsset)
                        {
                            string scenePath = AssetDatabase.GetAssetPath(obj);
                            AddSceneToBuild(scenePath);
                            addedScenes = true;
                        }
                    }
                }
                
                if (addedScenes)
                {
                    DragAndDrop.AcceptDrag();
                    evt.Use();
                }
                break;
        }
    }
    
    void HandleDragAndDropArea()
    {
        Event evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        
        GUI.Box(dropArea, "📁 Drag Scene Files Here to Add to Build Settings", EditorStyles.helpBox);
        
        switch (evt.type)
        {
            case EventType.DragUpdated:
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                }
                break;
                
            case EventType.DragPerform:
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.AcceptDrag();
                    
                    if (DragAndDrop.objectReferences != null)
                    {
                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is SceneAsset)
                            {
                                string scenePath = AssetDatabase.GetAssetPath(draggedObject);
                                AddSceneToBuild(scenePath);
                            }
                        }
                    }
                    evt.Use();
                }
                break;
        }
    }
    
    void HandleSceneDragAndDrop(int sceneIndex, Rect sceneRect)
    {
        Event evt = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        
        switch (evt.type)
        {
            case EventType.MouseDown:
                if (sceneRect.Contains(evt.mousePosition) && evt.button == 0)
                {
                    GUIUtility.hotControl = controlID;
                    draggedSceneIndex = sceneIndex;
                    isDragging = false;
                    evt.Use();
                }
                break;
                
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID)
                {
                    isDragging = true;
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.StartDrag("Scene Reorder");
                    evt.Use();
                }
                break;
                
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    
                    if (isDragging && draggedSceneIndex != sceneIndex && draggedSceneIndex >= 0)
                    {
                        ReorderScene(draggedSceneIndex, sceneIndex);
                    }
                    
                    isDragging = false;
                    draggedSceneIndex = -1;
                    evt.Use();
                }
                break;
                
            case EventType.DragUpdated:
                if (sceneRect.Contains(evt.mousePosition) && isDragging)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    evt.Use();
                }
                break;
                
            case EventType.DragPerform:
                if (sceneRect.Contains(evt.mousePosition) && isDragging)
                {
                    DragAndDrop.AcceptDrag();
                    if (draggedSceneIndex != sceneIndex && draggedSceneIndex >= 0)
                    {
                        ReorderScene(draggedSceneIndex, sceneIndex);
                    }
                    isDragging = false;
                    draggedSceneIndex = -1;
                    evt.Use();
                }
                break;
                
            case EventType.Repaint:
                if (isDragging && draggedSceneIndex == sceneIndex)
                {
                    EditorGUI.DrawRect(sceneRect, new Color(1, 1, 0, 0.3f));
                }
                break;
        }
    }
    
    void AddSceneToBuild(string scenePath)
    {
        List<EditorBuildSettingsScene> scenesList = EditorBuildSettings.scenes.ToList();
        
        // Check if scene already exists in build settings
        bool sceneExists = scenesList.Any(scene => scene.path == scenePath);
        
        if (!sceneExists)
        {
            scenesList.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenesList.ToArray();
            RefreshScenesList();
            Repaint();
        }
    }
    
    void RemoveSceneFromBuild(int index)
    {
        if (index >= 0 && index < cachedScenes.Length)
        {
            List<EditorBuildSettingsScene> scenesList = EditorBuildSettings.scenes.ToList();
            scenesList.RemoveAt(index);
            EditorBuildSettings.scenes = scenesList.ToArray();
            
            RefreshScenesList();
            Repaint();
        }
    }
    
    void ReorderScene(int fromIndex, int toIndex)
    {
        List<EditorBuildSettingsScene> scenesList = EditorBuildSettings.scenes.ToList();
        
        if (fromIndex >= 0 && fromIndex < scenesList.Count && toIndex >= 0 && toIndex < scenesList.Count && fromIndex != toIndex)
        {
            EditorBuildSettingsScene temp = scenesList[fromIndex];
            scenesList.RemoveAt(fromIndex);
            scenesList.Insert(toIndex, temp);
            
            EditorBuildSettings.scenes = scenesList.ToArray();
            RefreshScenesList();
            Repaint();
        }
    }

    private Texture2D MakeColorTexture(int width, int height, Color color) {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++) {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
}