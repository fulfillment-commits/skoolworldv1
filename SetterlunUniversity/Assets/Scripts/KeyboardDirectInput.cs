using UnityEngine;
using Invector.vCharacterController;
using UnityStandardAssets.CrossPlatformInput;

[DisallowMultipleComponent]
public class KeyboardDirectInput : MonoBehaviour
{
    private vThirdPersonController cc;
    private vThirdPersonInput tpInput;
    private bool useKeyboard;

    private float jumpDelay = 1f;
    private float nextJumpTime = 0f;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // When player is re-enabled, reinitialize everything
        Initialize();
    }

    void InitializeAgainAfterWait()
    {
        Initialize();
    }

    private void Initialize()
    {
        cc = GetComponent<vThirdPersonController>();
        tpInput = GetComponent<vThirdPersonInput>();

        if (!cc)
        {
            Debug.LogError("KeyboardDirectInput requires vThirdPersonController");
            return;
        }

        DetectPlatform();
    }

    private void DetectPlatform()
    {
#if UNITY_WEBGL
        bool isMobile =
            Application.isMobilePlatform ||
            SystemInfo.deviceType == DeviceType.Handheld;

        useKeyboard = !isMobile;
#else
        useKeyboard = true;
#endif

        Debug.Log("Input Mode: " + (useKeyboard ? "Keyboard (Desktop)" : "Mobile"));
    }

    private void Update()
    {
        if (!cc || !gameObject.activeInHierarchy) return;

        Vector3 input = Vector3.zero;

        if (useKeyboard)
        {
            float x = 0f;
            float z = 0f;

            if (Input.GetKey(KeyCode.A)) x = -1f;
            if (Input.GetKey(KeyCode.D)) x = 1f;
            if (Input.GetKey(KeyCode.W)) z = 1f;
            if (Input.GetKey(KeyCode.S)) z = -1f;

            input = new Vector3(x, 0f, z);

            if (input.magnitude > 1f)
                input.Normalize();

            cc.input = input;

            // Jump
            //if (Input.GetKeyDown(KeyCode.Space))
            //    cc.Jump();

            // Jump with delay
            if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextJumpTime)
            {
                cc.Jump();
                nextJumpTime = Time.time + jumpDelay;
            }

            // Crouch
            if (Input.GetKeyDown(KeyCode.C))
                cc.Crouch();

            // Sprint (hold style)
            cc.Sprint(Input.GetKey(KeyCode.LeftShift));

            // Hard reset axes
            if (x == 0f)
                CrossPlatformInputManager.SetAxisZero("Horizontal");

            if (z == 0f)
                CrossPlatformInputManager.SetAxisZero("Vertical");
        }
        else
        {
            float horizontal = CrossPlatformInputManager.GetAxisRaw("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxisRaw("Vertical");

            input = new Vector3(horizontal, 0f, vertical);

            if (input.magnitude > 1f)
                input.Normalize();

            cc.input = input;

            if (CrossPlatformInputManager.GetButtonDown("Jump"))
                cc.Jump();
        }
    }
}