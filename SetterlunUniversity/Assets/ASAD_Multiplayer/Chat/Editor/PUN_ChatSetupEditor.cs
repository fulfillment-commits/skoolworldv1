using ASAD_Multiplyer.Chat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ASAD_Multiplyer.CustomEditor
{
    public static class PUN_ChatSetupEditor
    {
        [MenuItem("ASAD Multiplayer/Chat/Create Or Refresh PUN Chat UI")]
        public static void CreateOrRefreshChatUi()
        {
            PUN_ChatManager manager = Object.FindObjectOfType<PUN_ChatManager>(true);
            if (manager == null)
            {
                GameObject managerObject = new GameObject("PUN_ChatManager");
                manager = managerObject.AddComponent<PUN_ChatManager>();
                Undo.RegisterCreatedObjectUndo(managerObject, "Create PUN Chat Manager");
            }

            RefreshManagerChildren(manager.transform);
            PUN_ChatRuntimeUiBuilder.Build(manager);

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Selection.activeGameObject = manager.gameObject;

            Debug.Log("[PUN Chat Setup] Created/refreshed PUN chat UI. Run this in the Onboarding scene if you want the scene object saved there.");
        }

        [MenuItem("ASAD Multiplayer/Chat/Select PUN Chat Manager")]
        private static void SelectChatManager()
        {
            PUN_ChatManager manager = Object.FindObjectOfType<PUN_ChatManager>(true);
            if (manager != null)
            {
                Selection.activeGameObject = manager.gameObject;
            }
            else
            {
                Debug.LogWarning("[PUN Chat Setup] No PUN_ChatManager found in the current scene.");
            }
        }

        private static void RefreshManagerChildren(Transform managerTransform)
        {
            for (int i = managerTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = managerTransform.GetChild(i);
                if (child != null && child.name.StartsWith("PUN_Chat"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }
        }
    }
}
