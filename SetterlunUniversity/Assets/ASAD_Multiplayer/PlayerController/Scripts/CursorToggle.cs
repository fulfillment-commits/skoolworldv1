using UnityEngine;
using Invector.vCharacterController;

public class CursorToggle : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.LeftControl; // Or RightControl
    private bool cursorActive = false;
    private vThirdPersonInput playerController;

    void Start()
    {
        #if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        toggleKey = KeyCode.LeftApple;
        #else
        toggleKey = KeyCode.LeftControl;
        #endif
        cursorActive = false;
        playerController = gameObject.GetComponent<vThirdPersonInput>();
        SetCursor();
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.LeftApple) || Input.GetKeyDown(KeyCode.LeftCommand))
        {
            cursorActive = !cursorActive;
            SetCursor();
            
        }
    }

    void SetCursor()
    {
        if (cursorActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // ChatCanvas.Instance.txtCtrlInput.text = "Cursor Unlocked";
            if (playerController != null)
                playerController.SetLockCameraInput(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // ChatCanvas.Instance.txtCtrlInput.text = "Cursor Locked";
            if (playerController != null)
                playerController.SetLockCameraInput(false);
        }
        
        // ChatCanvas.Instance.txtCtrlInput.transform.parent.gameObject.SetActive(true);
    }
}