using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LoginScreen : ScreenBase
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField loginInput;      // Can be Email OR Username
    [SerializeField] private TMP_InputField passwordInput;

    [Header("Password Toggle")]
    [SerializeField] private Toggle passwordToggle;
    [SerializeField] private Toggle rememberMeToggle;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button forgotPasswordButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    private bool isPasswordVisible = false;
    private bool pendingRememberMe = true;
    private const string PLAYERPREFS_REMEMBER_ME = "OnboardingRememberMe";

    protected override void OnShow()
    {
        base.OnShow();
        ClearStatus();
        ResetForm();
        loginInput.Select();
    }

    protected override void OnHide()
    {
        base.OnHide();
        passwordInput.text = "";
        passwordToggle.isOn = false;
    }

    private void Start()
    {
        SetupPasswordToggle();
        SetupRememberMeToggle();

        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
        forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
    }

    private void SetupPasswordToggle()
    {
        if (passwordToggle != null)
        {
            passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);
            passwordToggle.isOn = false;
        }
    }

    private void SetupRememberMeToggle()
    {
        if (rememberMeToggle == null)
        {
            rememberMeToggle = FindRememberMeToggle();
        }

        if (rememberMeToggle != null)
        {
            rememberMeToggle.isOn = PlayerPrefs.GetInt(PLAYERPREFS_REMEMBER_ME, 1) == 1;
        }
    }

    private Toggle FindRememberMeToggle()
    {
        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
        foreach (Toggle toggle in toggles)
        {
            if (toggle != null && toggle != passwordToggle)
            {
                return toggle;
            } 
        } 

        return null;
    }

    private void OnPasswordToggleChanged(bool isVisible)
    {
        var contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        passwordInput.contentType = contentType;
        passwordInput.ForceLabelUpdate();
    }

    private void OnLoginClicked()
    {
        if (!ValidateInputs()) return;

        ShowLoading("Logging in...");
        pendingRememberMe = rememberMeToggle == null || rememberMeToggle.isOn;
        BackendSettings.Instance.Service?.SetRememberMe(pendingRememberMe);

        BackendSettings.Instance.Service.Login(
            loginInput.text.Trim(),
            passwordInput.text,
            onSuccess: HandleSuccessfulBackendLogin,
            onError: HandleLoginError
        );
    }

    private void HandleSuccessfulBackendLogin(BackendResponse response)
    { 
        ShowSuccess("Login successful! Welcome back.");

        if (!string.IsNullOrEmpty(response.userId))
        {
            // Save avatar index for the selection screen
            PlayerPrefs.SetInt("OnboardingAvatarIndex", response.avatar_index);
            PlayerPrefs.Save();

            // Create Session
            OnboardingManager.Instance?.Initialize(response.userId, response.username, response.email);
            OnboardingManager.Instance?.SetRememberMe(pendingRememberMe);

            Invoke(nameof(GoToNextScreen), 1.2f);
        }
        else
        {
            HandleLoginError("User ID not found in response.");
        }
    }

    private void HandleLoginError(string errorMessage)
    {
        loginButton.interactable = true;
        ShowError(errorMessage);
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(loginInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
        {
            ShowError("Please enter your email/username and password");
            return false;
        }

        //if (passwordInput.text.Length < 6)
        //{
        //    ShowError("Password must be at least 6 characters");
        //    return false;
        //}

        return true;
    }

    private void GoToNextScreen()
    {
        ScreenManager.Instance.ShowScreen(ScreenType.FastAvatar);
    }

    private void OnRegisterClicked()
    {
        if (OnboardingManager.Instance != null)
        {
            OnboardingManager.Instance.StartJourney();
        }
        else
        {
            // Fallback if OnboardingManager is missing
            ScreenManager.Instance.ShowScreen(ScreenType.FastRegister);
        }
    }

    private void OnForgotPasswordClicked()
    {
        ShowError("Forgot Password feature is coming soon...");
    }

    // ====================== UI Helpers ======================
    private void ShowLoading(string msg)
    {
        statusText.text = msg;
        statusText.color = Color.yellow;
        loginButton.interactable = false;
    }

    private void ShowSuccess(string msg)
    {
        statusText.text =  msg;
        statusText.color = Color.green;
        loginButton.interactable = true;
    }

    private void ShowError(string msg)
    {
        statusText.text =  msg;
        statusText.color = Color.red;
        loginButton.interactable = true;
    }

    private void ClearStatus()
    {
        statusText.text = "";
        loginButton.interactable = true;
    }

    private void ResetForm()
    {
        loginInput.text = "";
        passwordInput.text = "";
        passwordToggle.isOn = false;
        if (rememberMeToggle != null)
        {
            rememberMeToggle.isOn = PlayerPrefs.GetInt(PLAYERPREFS_REMEMBER_ME, 1) == 1;
        }
        ClearStatus();
    }
}
