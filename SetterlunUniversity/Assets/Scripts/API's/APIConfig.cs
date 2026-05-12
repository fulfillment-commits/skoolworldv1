using UnityEngine;

public static class ApiConfig
{
    public static bool UseLocalDemoMode => GameSettings.Instance != null
        ? GameSettings.Instance.UseLocalDemoMode
        : false;

    public static string BaseUrl
    {
        get
        {
            if (UseLocalDemoMode)
                return "LOCAL_DEMO_MODE";   // Marker for demo mode

            if (GameSettings.Instance != null)
                return GameSettings.Instance.BaseUrl;

            // Fallback for editor or when GameSettings is missing
            return "http://localhost:5000";
        }
    }

    // ====================== AUTH ======================
    public static string AuthRegister => $"{BaseUrl}/auth/register";
    public static string AuthLogin => $"{BaseUrl}/auth/login";

    // ====================== USERS ======================
    public static string Users => $"{BaseUrl}/users";

    // ====================== ONBOARDING ======================
    public static string OnboardingSteps => $"{BaseUrl}/onboarding-steps";

    // ====================== PROFILES ======================
    public static string PersonalProfiles => $"{BaseUrl}/personal-profiles";
    public static string BusinessProfiles => $"{BaseUrl}/business-profiles";

    // ====================== COMPANY ======================
    public static string Companies => $"{BaseUrl}/companies";
    public static string CompanyMembers => $"{BaseUrl}/company-members";

    // ====================== BRICKS ======================
    public static string Bricks => $"{BaseUrl}/bricks";

    // ====================== COURSES ======================
    public static string Courses => $"{BaseUrl}/courses";
    public static string UserCourseAssignments => $"{BaseUrl}/user-course-assignments";

    // ====================== TIME CAPSULE ======================
    public static string TimeCapsules => $"{BaseUrl}/time-capsules";

    // Helper to get full URL for any route
    public static string GetUrl(string endpoint)
    {
        return $"{BaseUrl}/{endpoint.TrimStart('/')}";
    }
}