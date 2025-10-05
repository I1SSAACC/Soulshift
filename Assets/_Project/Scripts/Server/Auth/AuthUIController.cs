using System;
using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthUIController : MonoBehaviour
{
    public static AuthUIController Instance { get; private set; }

    // Public constants

    // Private constants

    // Serialized fields
    [Header("Registration Panel")]
    [SerializeField] private GameObject _regPanel;
    [SerializeField] private TMP_InputField _regEmail;
    [SerializeField] private TMP_InputField _regNickname;
    [SerializeField] private TMP_InputField _regPassword;
    [SerializeField] private Button _regButton;
    [SerializeField] private TMP_Text _regFeedback;

    [Header("Login Panel")]
    [SerializeField] private GameObject _loginPanel;
    [SerializeField] private TMP_InputField _loginNickname;
    [SerializeField] private TMP_InputField _loginPassword;
    [SerializeField] private Button _loginButton;
    [SerializeField] private TMP_Text _loginFeedback;

    [Header("Gameplay")]
    [SerializeField] private GameObject _panelLoading;
    [SerializeField] private Button _startGameButton;

    [Header("First Run")]
    [SerializeField] private Button _authToggleButton;
    [SerializeField] private GameObject _authRootPanel;

    // Constructors (none)

    // Public properties
    public bool IsAuthenticated => _isLoggedIn;

    // Private properties

    // Public Unity methods

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (ValidateInspectorFields() == false)
        {
            Debug.LogError("[AuthUI] Missing inspector references. Disabling AuthUIController.");
            enabled = false;
            return;
        }

        NetworkClient.RegisterHandler<RegisterResponseMessage>(OnRegisterResponse, false);
        NetworkClient.RegisterHandler<LoginResponseMessage>(OnLoginResponse, false);

        NetworkClient.OnDisconnectedEvent += OnClientDisconnected;
    }

    private void Start()
    {
        _regButton.onClick.AddListener(OnRegisterClicked);
        _loginButton.onClick.AddListener(OnLoginClicked);

        if (_startGameButton != null)
            _startGameButton.onClick.AddListener(OnStartGameClicked);

        if (_authToggleButton != null)
            _authToggleButton.onClick.AddListener(OnAuthToggleClicked);

        AuthRequestData.Type = AuthType.None;

        // First-run behaviour: show auth toggle button only once; subsequent runs will auto-connect
        if (PlayerPrefs.GetInt("HasSeenAuthToggle", 0) == 0)
        {
            if (_authRootPanel != null)
                _authRootPanel.SetActive(false);

            if (_authToggleButton != null)
                _authToggleButton.gameObject.SetActive(true);
        }
        else
        {
            if (_authToggleButton != null)
                _authToggleButton.gameObject.SetActive(false);

            // Automatic connect intent for subsequent runs: prepare Auto auth
            AuthRequestData.Type = AuthType.Auto;
        }
    }

    private bool ValidateInspectorFields()
    {
        if (_regPanel == null || _regEmail == null || _regNickname == null || _regPassword == null || _regButton == null || _regFeedback == null)
            return false;

        if (_loginPanel == null || _loginNickname == null || _loginPassword == null || _loginButton == null || _loginFeedback == null)
            return false;

        return true;
    }

    private void OnDestroy()
    {
        NetworkClient.UnregisterHandler<RegisterResponseMessage>();
        NetworkClient.UnregisterHandler<LoginResponseMessage>();
        NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;

        if (_regButton != null)
            _regButton.onClick.RemoveListener(OnRegisterClicked);

        if (_loginButton != null)
            _loginButton.onClick.RemoveListener(OnLoginClicked);

        if (_startGameButton != null)
            _startGameButton.onClick.RemoveListener(OnStartGameClicked);

        if (_authToggleButton != null)
            _authToggleButton.onClick.RemoveListener(OnAuthToggleClicked);

        if (_connectCoroutine != null)
        {
            StopCoroutine(_connectCoroutine);
            _connectCoroutine = null;
        }
    }

    // Public custom methods

    public void ShowLoginPanel(string message = "")
    {
        if (_regPanel != null)
            _regPanel.SetActive(false);

        if (_loginPanel != null)
            _loginPanel.SetActive(true);

        if (_loginFeedback != null)
            _loginFeedback.text = message;
    }

    public void ShowRegisterPanel(string message = "")
    {
        if (_loginPanel != null)
            _loginPanel.SetActive(false);

        if (_regPanel != null)
            _regPanel.SetActive(true);

        if (_regFeedback != null)
            _regFeedback.text = message;
    }

    public void StartGame()
    {
        if (_isLoggedIn)
        {
            Debug.Log("[AuthUI] StartGame: user already logged in, proceeding to game");
            return;
        }

        AuthRequestData.Type = AuthType.Auto;

        if (NetworkClient.isConnected == false)
        {
            if (_isConnecting == false)
            {
                _isConnecting = true;
                NetworkManager.singleton.StartClient();
            }

            if (_connectCoroutine != null)
                StopCoroutine(_connectCoroutine);

            _connectCoroutine = StartCoroutine(WaitForConnectionThen(() =>
            {
                // OnClientConnect will handle AutoLoginRequest
            }, 10f));
        }
        else
        {
            NetworkClient.Send(new AutoLoginRequestMessage { deviceId = DeviceIdHelper.GetLocalDeviceId() });
            Debug.Log("[AuthUI] StartGame: sent AutoLoginRequest (already connected)");
        }
    }

    public void HandleLoggedOut()
    {
        if (NetworkClient.isConnected)
        {
            try
            {
                NetworkClient.Send(new LogoutRequestMessage { deviceId = DeviceIdHelper.GetLocalDeviceId() });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AuthUI] Failed to send logout request: {ex}");
            }
        }

        _isLoggedIn = false;

        AuthRequestData.Type = AuthType.None;
        AuthRequestData.Nickname = null;
        AuthRequestData.Password = null;
        AuthRequestData.RememberMe = false;

        if (NetworkManager.singleton != null && NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();

        ShowLoginPanel();
    }

    // Private Unity methods

    private void OnRegisterClicked()
    {
        if (_regEmail == null || _regNickname == null || _regPassword == null || _regFeedback == null)
            return;

        if (_isWaitingForRegister)
        {
            _regFeedback.text = "Request already in progress. Please wait.";
            return;
        }

        if (string.IsNullOrEmpty(_regEmail.text) || string.IsNullOrEmpty(_regNickname.text) || string.IsNullOrEmpty(_regPassword.text))
        {
            _regFeedback.text = "Please fill out all fields";
            return;
        }

        _isWaitingForRegister = true;
        _regFeedback.text = "Registering...";

        string email = _regEmail.text;
        string nickname = _regNickname.text;
        string plainPassword = _regPassword.text;

        _pendingRegisteredPassword = plainPassword;

        // prepare to login after registration if possible
        AuthRequestData.Type = AuthType.Login;
        AuthRequestData.Nickname = nickname;
        AuthRequestData.Password = plainPassword;
        AuthRequestData.RememberMe = false;

        if (NetworkClient.isConnected == false)
        {
            if (_isConnecting == false)
            {
                _isConnecting = true;
                NetworkManager.singleton.StartClient();
            }

            if (_connectCoroutine != null)
                StopCoroutine(_connectCoroutine);

            _connectCoroutine = StartCoroutine(WaitForConnectionThen(() => SendRegister(email, nickname, plainPassword), 10f));
        }
        else
        {
            SendRegister(email, nickname, plainPassword);
        }
    }

    private void OnLoginClicked()
    {
        if (_loginNickname == null || _loginPassword == null || _loginFeedback == null)
            return;

        if (string.IsNullOrEmpty(_loginNickname.text) || string.IsNullOrEmpty(_loginPassword.text))
        {
            _loginFeedback.text = "Please enter username and password";
            return;
        }

        AuthRequestData.Type = AuthType.Login;
        AuthRequestData.Nickname = _loginNickname.text;
        AuthRequestData.Password = _loginPassword.text;
        AuthRequestData.RememberMe = false;

        _loginFeedback.text = "Logging in…";

        if (NetworkClient.isConnected == false)
        {
            if (_isConnecting == false)
            {
                _isConnecting = true;
                NetworkManager.singleton.StartClient();
            }

            if (_connectCoroutine != null)
                StopCoroutine(_connectCoroutine);

            _connectCoroutine = StartCoroutine(WaitForConnectionThen(null, 10f));
            return;
        }

        LoginRequestMessage immediateLogin = new LoginRequestMessage
        {
            nickname = AuthRequestData.Nickname,
            passwordHash = HashUtility.SHA512(AuthRequestData.Password),
            deviceId = DeviceIdHelper.GetLocalDeviceId(),
            rememberMe = AuthRequestData.RememberMe
        };

        AuthRequestData.Password = null;

        NetworkClient.Send(immediateLogin);
    }

    private void OnStartGameClicked()
    {
        StartGame();
    }

    // Private custom methods

    private bool _isWaitingForRegister;
    private bool _isConnecting;
    private Coroutine _connectCoroutine;
    private bool _isLoggedIn;
    private string _pendingRegisteredPassword;

    private IEnumerator WaitForConnectionThen(System.Action action, float timeoutSeconds)
    {
        float t = 0f;
        while (NetworkClient.isConnected == false && t < timeoutSeconds)
        {
            t += Time.deltaTime;
            yield return null;
        }

        _connectCoroutine = null;
        _isConnecting = NetworkClient.isConnected;

        if (NetworkClient.isConnected == false)
        {
            _isWaitingForRegister = false;
            if (_regFeedback != null)
                _regFeedback.text = "Failed to connect. Please try again.";

            ClearInputPassword();
            yield break;
        }

        action?.Invoke();
    }

    private void SendRegister(string email, string nickname, string plainPassword)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(plainPassword))
        {
            _isWaitingForRegister = false;
            if (_regFeedback != null)
                _regFeedback.text = "Registration data missing.";
            return;
        }

        RegisterRequestMessage msg = new RegisterRequestMessage
        {
            email = email,
            nickname = nickname,
            passwordHash = HashUtility.SHA512(plainPassword),
            deviceId = DeviceIdHelper.GetLocalDeviceId()
        };

        NetworkClient.Send(msg);
    }

    private void ClearInputPassword()
    {
        if (_regPassword != null)
            _regPassword.text = string.Empty;

        if (_loginPassword != null)
            _loginPassword.text = string.Empty;
    }

    private void OnRegisterResponse(RegisterResponseMessage msg)
    {
        _isWaitingForRegister = false;

        Debug.Log($"[AuthUI] RegisterResponse: success={msg.success}, msg={msg.message}");

        if (_regFeedback != null)
            _regFeedback.text = msg.message;

        if (msg.success == false)
            return;

        if (_loginFeedback != null)
            _loginFeedback.text = "Registered! Logging in…";

        // If registration succeeded and we prepared AuthRequestData earlier, try immediate login.
        if (string.IsNullOrEmpty(_pendingRegisteredPassword) == false && string.IsNullOrEmpty(_regNickname.text) == false)
        {
            AuthRequestData.Type = AuthType.Login;
            AuthRequestData.Nickname = _regNickname.text;
            AuthRequestData.Password = _pendingRegisteredPassword;
            AuthRequestData.RememberMe = false;

            // If connected already, send immediate login request.
            if (NetworkClient.isConnected)
            {
                LoginRequestMessage loginMsg = new LoginRequestMessage
                {
                    nickname = AuthRequestData.Nickname,
                    passwordHash = HashUtility.SHA512(_pendingRegisteredPassword),
                    deviceId = DeviceIdHelper.GetLocalDeviceId(),
                    rememberMe = AuthRequestData.RememberMe
                };

                AuthRequestData.Password = null;

                NetworkClient.Send(loginMsg);
                Debug.Log("[AuthUI] Sent immediate login after registration");
            }
            else
            {
                // ensure client will send login on connect (OnClientAuthenticate will handle it)
                if (_isConnecting == false)
                {
                    _isConnecting = true;
                    NetworkManager.singleton.StartClient();
                }

                if (_connectCoroutine != null)
                    StopCoroutine(_connectCoroutine);

                _connectCoroutine = StartCoroutine(WaitForConnectionThen(null, 10f));
            }
        }

        _pendingRegisteredPassword = null;

        if (_regPassword != null)
            _regPassword.text = string.Empty;

        // mark that user has seen auth toggle
        PlayerPrefs.SetInt("HasSeenAuthToggle", 1);
        PlayerPrefs.Save();

        AuthRequestData.Type = AuthType.None;
        AuthRequestData.Nickname = null;
        AuthRequestData.Password = null;
    }

    private void OnLoginResponse(LoginResponseMessage msg)
    {
        Debug.Log($"[AuthUI] LoginResponse: success={msg.success}, msg={msg.message}");

        if (msg.success == false)
        {
            ShowLoginPanel(msg.message);
            return;
        }

        _isLoggedIn = true;
        ClearInputPassword();

        // mark that user has seen auth toggle
        PlayerPrefs.SetInt("HasSeenAuthToggle", 1);
        PlayerPrefs.Save();

        if (_panelLoading != null)
            _panelLoading.SetActive(false);
    }

    private void OnClientDisconnected()
    {
        ShowLoginPanel("Disconnected from server");
        _isConnecting = false;
    }

    private void OnAuthToggleClicked()
    {
        if (_authRootPanel != null)
            _authRootPanel.SetActive(true);

        if (_authToggleButton != null)
            _authToggleButton.gameObject.SetActive(false);

        PlayerPrefs.SetInt("HasSeenAuthToggle", 1);
        PlayerPrefs.Save();
    }
}