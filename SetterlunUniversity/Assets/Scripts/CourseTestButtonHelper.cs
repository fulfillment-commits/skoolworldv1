using UnityEngine;

/// <summary>
/// Tiny runtime helper attached to test buttons so they can call
/// ScreenManager at runtime without needing a scene reference baked in.
/// Safe to delete together with the test buttons before shipping.
/// </summary>
public class CourseTestButtonHelper : MonoBehaviour
{
    public ScreenType targetScreen = ScreenType.Course_BusinessPortal;

    public void Open()
    {
        if (ScreenManager.Instance != null)
            ScreenManager.Instance.ShowScreen(targetScreen);
        else
            Debug.LogError("[CourseTestButtonHelper] ScreenManager.Instance is null.");
    }
}
