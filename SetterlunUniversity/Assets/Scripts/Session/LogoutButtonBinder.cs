using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LogoutButtonBinder : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.RemoveListener(Logout);
        button.onClick.AddListener(Logout);
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Logout);
        }
    }

    private static void Logout()
    {
        if (LogoutController.Instance != null)
        {
            LogoutController.Instance.LogoutAndRestart();
        }
    }
}
