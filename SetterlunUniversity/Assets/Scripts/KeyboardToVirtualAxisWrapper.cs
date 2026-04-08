using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class KeyboardToVirtualAxis : MonoBehaviour
{
    public GameObject mobileJoystickUI; // Drag your joystick canvas here

    void Start()
    {
#if UNITY_WEBGL
        bool isMobile =
            Application.isMobilePlatform ||
            SystemInfo.deviceType == DeviceType.Handheld;

        if (mobileJoystickUI != null)
            mobileJoystickUI.SetActive(isMobile);
#else
        if (mobileJoystickUI != null)
            mobileJoystickUI.SetActive(false);
#endif
    }
}