using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WebGLCheckIsMobile : MonoBehaviour
{
    private void OnEnable()
    {
        #if UNITY_WEBGL
        // Desktop browser → keyboard, Mobile browser → joystick
            if (Application.isMobilePlatform ||
            SystemInfo.deviceType == DeviceType.Handheld)
            {   
                
            }
        else
        {
            gameObject.SetActive(false); // Disable this GameObject if on desktop
        }
        #endif
    }
}
