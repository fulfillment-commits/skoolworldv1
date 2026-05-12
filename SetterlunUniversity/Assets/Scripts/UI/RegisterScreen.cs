using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class RegisterScreen : ScreenBase
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField fullNameInput;
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField phoneInput;

    [Header("Timezone Field (Read Only)")]
    [SerializeField] private TMP_InputField timezoneInput;

    [Header("Password Fields")]
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    [Header("Password Toggle")]
    [SerializeField] private Toggle passwordToggle;

    [Header("Discovery Source")]
    [SerializeField] private TMP_Dropdown discoveryDropdown;

    [Header("Buttons")]
    [SerializeField] private Button registerButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button forgotPasswordButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    private bool isPasswordVisible = false;

    // ==================== ScreenBase Overrides ====================
    protected override void OnShow()
    {
        base.OnShow();
        ClearStatus();
        ResetForm();
        AutoFillTimezone();           // ← This method was missing
        fullNameInput.Select();
    }

    protected override void OnHide()
    {
        base.OnHide();
        passwordInput.text = "";
        confirmPasswordInput.text = "";
        passwordToggle.isOn = false;
    }

    private void Start()
    {
        SetupDiscoveryDropdown();
        SetupPasswordToggle();

        // Real-time email validation
        emailInput.onValueChanged.AddListener(OnEmailValueChanged);

        registerButton.onClick.AddListener(OnRegisterClicked);
        loginButton.onClick.AddListener(OnLoginClicked);
        forgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
    }

    // ====================== Auto-detect & Show Timezone ======================
    private void AutoFillTimezone()
    {
        if (timezoneInput != null)
        {
            string detectedTimezone = TimezoneHelper.GetCurrentTimezone();
            timezoneInput.text = detectedTimezone;
            timezoneInput.interactable = false;           // Read-only
            // Optional: Light gray background to show it's disabled
            if (timezoneInput.image != null)
                timezoneInput.image.color = new Color(0.95f, 0.95f, 0.95f);
        }
    }

    private void SetupDiscoveryDropdown()
    {
        discoveryDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>
        {
            "Select how you found us...",
            "Instagram", "Facebook", "YouTube", "TikTok",
            "Google Search", "Friend Referral", "Twitter / X",
            "LinkedIn", "Other"
        };
        discoveryDropdown.AddOptions(options);
    }

    private void SetupPasswordToggle()
    {
        if (passwordToggle != null)
        {
            passwordToggle.onValueChanged.AddListener(OnPasswordToggleChanged);
            passwordToggle.isOn = false;
        }
    }

    private void OnPasswordToggleChanged(bool isVisible)
    {
        var contentType = isVisible
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        passwordInput.contentType = contentType;
        confirmPasswordInput.contentType = contentType;
        passwordInput.ForceLabelUpdate();
        confirmPasswordInput.ForceLabelUpdate();
    }

    // ====================== REAL-TIME EMAIL VALIDATION ======================
    private void OnEmailValueChanged(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ClearEmailFeedback();
            return;
        }

        if (IsValidEmail(email))
        {
            ClearEmailFeedback();
        }
        else
        {
            statusText.text = "❌ Please enter a valid email address";
            statusText.color = Color.red;
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        string pattern = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
        return Regex.IsMatch(email, pattern);
    }

    private void ClearEmailFeedback()
    {
        if (statusText.text.Contains("email"))
            statusText.text = "";
    }

    private void OnRegisterClicked()
    {
        if (!ValidateInputs()) return;

        ShowLoading("Creating your account...");

        string selectedDiscovery = discoveryDropdown.options[discoveryDropdown.value].text;
        if (selectedDiscovery == "Select how you found us...")
            selectedDiscovery = "Other";

        UserData userData = new UserData
        {
            full_name = fullNameInput.text.Trim(),
            username = usernameInput.text.Trim(),
            email = emailInput.text.Trim().ToLower(),
            phone = string.IsNullOrEmpty(phoneInput.text) ? null : phoneInput.text.Trim(),
            password = passwordInput.text
        };

        BackendSettings.Instance.Service.Register(userData,
            onSuccess: HandleSuccessfulBackendRegistration,
            onError: HandleRegistrationError
        );
    }

    private void HandleSuccessfulBackendRegistration(BackendResponse response)
    {
        ShowSuccess("Account created successfully!");

        if (response != null && !string.IsNullOrEmpty(response.userId))
        {
            // Initialize Managers
            OnboardingManager.Instance?.Initialize(response.userId, response.username, response.email);

            Invoke(nameof(GoToNextScreen), 1.3f);
        }
    }

    private void HandleRegistrationError(string errorMessage)
    {
        registerButton.interactable = true;

        if (errorMessage.Contains("already exists") || errorMessage.Contains("duplicate"))
        {
            ShowError("Username or Email is already registered.\nPlease use a different one or try Login.");
        }
        else
        {
            ShowError("Registration failed. Please try again.\n" + errorMessage);
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(fullNameInput.text) ||
            string.IsNullOrWhiteSpace(usernameInput.text) ||
            string.IsNullOrWhiteSpace(emailInput.text) ||
            string.IsNullOrWhiteSpace(passwordInput.text))
        {
            ShowError("Please fill all required fields (*)");
            return false;
        }

        if (!IsValidEmail(emailInput.text.Trim()))
        {
            ShowError("Please enter a valid email address");
            return false;
        }

        if (passwordInput.text != confirmPasswordInput.text)
        {
            ShowError("Passwords do not match!");
            return false;
        }

        if (passwordInput.text.Length < 6)
        {
            ShowError("Password must be at least 6 characters");
            return false;
        }

        if (discoveryDropdown.value == 0)
        {
            ShowError("Please select how you discovered Setterlun World");
            return false;
        }

        return true;
    }

    private void GoToNextScreen()
    {
        OnboardingManager.Instance.ContinueToAvatar();
    }

    // ====================== UI Helpers ======================
    private void ShowLoading(string msg)
    {
        statusText.text = msg;
        statusText.color = Color.yellow;
        registerButton.interactable = false;
    }

    private void ShowSuccess(string msg)
    {
        statusText.text = "✅ " + msg;
        statusText.color = Color.green;
        registerButton.interactable = true;
    }

    private void ShowError(string msg)
    {
        statusText.text = "❌ " + msg;
        statusText.color = Color.red;
        registerButton.interactable = true;
    }

    private void ClearStatus()
    {
        statusText.text = "";
        registerButton.interactable = true;
    }

    private void ResetForm()
    {
        fullNameInput.text = "";
        usernameInput.text = "";
        emailInput.text = "";
        phoneInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
        discoveryDropdown.value = 0;
        passwordToggle.isOn = false;
        ClearStatus();
    }

    private void OnLoginClicked() => ScreenManager.Instance.ShowScreen(ScreenType.Login);
    private void OnForgotPasswordClicked() => ShowError("Forgot Password feature is coming soon...");
}