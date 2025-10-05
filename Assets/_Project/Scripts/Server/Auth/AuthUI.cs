using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class AuthUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _registerEmailInput;
    [SerializeField] private TMP_InputField _registerLoginInput;
    [SerializeField] private TMP_InputField _registerPasswordInput;
    [SerializeField] private Button _registerButton;

    [SerializeField] private TMP_InputField _loginLoginInput;
    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private Button _loginButton;

    [SerializeField] private TMP_Text _registerFeedbackText;
    [SerializeField] private TMP_Text _loginFeedbackText;

    private void Awake()
    {
        _registerButton.onClick.AddListener(OnRegisterButtonClicked);
        _loginButton.onClick.AddListener(OnLoginButtonClicked);
    }

    private void OnDestroy()
    {
        _registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        _loginButton.onClick.RemoveListener(OnLoginButtonClicked);
    }

    private void OnRegisterButtonClicked()
    {
        if (NetworkClient.isConnected == false)
        {
            _registerFeedbackText.text = "Not connected to server";
            return;
        }

        string email = _registerEmailInput.text;
        string login = _registerLoginInput.text;
        string password = _registerPasswordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            _registerFeedbackText.text = "Please fill all fields";
            return;
        }

        AuthRequest request = new AuthRequest
        {
            IsRegistration = true,
            Email = email,
            Login = login,
            Password = password,
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            Token = string.Empty
        };

        NetworkAuthBridge.SendAuthRequest(request);
    }

    private void OnLoginButtonClicked()
    {
        if (NetworkClient.isConnected == false)
        {
            _loginFeedbackText.text = "Not connected to server";
            return;
        }

        string login = _loginLoginInput.text;
        string password = _loginPasswordInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            _loginFeedbackText.text = "Please provide login and password";
            return;
        }

        AuthRequest request = new AuthRequest
        {
            IsRegistration = false,
            Email = string.Empty,
            Login = login,
            Password = password,
            DeviceId = SystemInfo.deviceUniqueIdentifier,
            Token = string.Empty
        };

        NetworkAuthBridge.SendAuthRequest(request);
    }

    public void ShowRegisterFeedback(string message) => _registerFeedbackText.text = message;

    public void ShowLoginFeedback(string message) => _loginFeedbackText.text = message;
}