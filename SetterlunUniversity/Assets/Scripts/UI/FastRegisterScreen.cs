using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class FastRegisterScreen : ScreenBase
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;
    [SerializeField] private TMP_InputField referralInput;

    [Header("Password Toggle")]
    [SerializeField] private Toggle passwordToggle;

    [Header("Buttons")]
    [SerializeField] private Button registerButton;
    [SerializeField] private Button backToLoginButton;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI statusText;

    private void Start()
    {
        SetupPasswordToggle();
        SetupEmailValidation();

        registerButton.onClick.AddListener(OnRegisterClicked);
        backToLoginButton.onClick.AddListener(OnBackToLoginClicked);
    }

    protected override void OnShow()
    {
        base.OnShow();
        ClearStatus();
        ResetForm();
        usernameInput.Select();
    }

    protected override void OnHide()
    {
        base.OnHide();
        passwordInput.text = "";
        confirmPasswordInput.text = "";
        if (passwordToggle != null) passwordToggle.isOn = false;
    }

    // ====================== Password Toggle ======================
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

    // ====================== Real-time Email Validation ======================
    private void SetupEmailValidation()
    {
        if (emailInput != null)
        {
            emailInput.onValueChanged.AddListener(OnEmailValueChanged);
        }
    }

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

    // ====================== Register Logic ======================
    private void OnRegisterClicked()
    {
        if (!ValidateInputs()) return;

        ShowLoading("Creating your account...");

        UserData userData = new UserData
        {
            username = usernameInput.text.Trim(),
            email = emailInput.text.Trim().ToLower(),
            password = passwordInput.text,
            full_name = usernameInput.text.Trim()
        };

        BackendSettings.Instance.Service.Register(userData,
            onSuccess: HandleSuccessfulRegistration,
            onError: HandleRegistrationError);
    }

    private void HandleSuccessfulRegistration(BackendResponse response)
    {
        ShowSuccess("Account created successfully!");

        if (response != null && !string.IsNullOrEmpty(response.userId))
        {
            string finalUsername = usernameInput.text.Trim();
            PlayerPrefs.SetString("CurrentUserId_Str", response.userId);
            PlayerPrefs.SetString("OnboardingUsername", finalUsername);
            PlayerPrefs.Save();

            OnboardingManager.Instance?.Initialize(response.userId, finalUsername, emailInput.text.Trim());

            Invoke(nameof(GoToFastAvatar), 1.3f);
        }
    }

    private void HandleRegistrationError(string errorMessage)
    {
        registerButton.interactable = true;

        if (errorMessage.Contains("already exists") || errorMessage.Contains("duplicate"))
        {
            ShowError("Username or Email already exists.\nPlease try Login instead.");
        }
        else
        {
            ShowError("Registration failed. Please try again.\n" + errorMessage);
        }
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(usernameInput.text) ||
            string.IsNullOrWhiteSpace(emailInput.text) ||
            string.IsNullOrWhiteSpace(passwordInput.text))
        {
            ShowError("Username, Email and Password are required");
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
            ShowError("Password must be at least 6 characters long");
            return false;
        }

        return true;
    }

    private void GoToFastAvatar()
    {
        OnboardingManager.Instance.ContinueToAvatar();
    }

    private void OnBackToLoginClicked()
    {
        ScreenManager.Instance.GoToLogin();
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
        usernameInput.text = "";
        emailInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
        referralInput.text = "";
        ClearStatus();
    }
}