using System.Collections;
using Mirror;
using PromoCodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PromoUIController : MonoBehaviour
{
    [Header("Promo Input Panel")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private GameObject _inputPanel;
    [SerializeField] private TMP_InputField _promoInput;
    [SerializeField] private Button _redeemButton;
    [SerializeField] private TMP_Text _feedbackText;

    [Header("Reward Panel")]
    [SerializeField] private CanvasGroup _rewardGroup;
    [SerializeField] private Image _rewardIcon;
    [SerializeField] private TMP_Text _rewardAmountText;
    [SerializeField] private Button _rewardOkButton;
    [SerializeField] private float _fadeDuration = 0.5f;

    [Header("Reward Sprites")]
    [SerializeField] private Sprite _goldSprite;
    [SerializeField] private Sprite _diamondsSprite;
    [SerializeField] private Sprite _tokensSprite;

    private void Awake()
    {
        NetworkClient.RegisterHandler<RedeemPromoResponse>(OnClientPromoResponse, false);
    }

    private void Start()
    {
        if (_redeemButton != null)
            _redeemButton.onClick.AddListener(OnRedeemClicked);

        if (_rewardOkButton != null)
            _rewardOkButton.onClick.AddListener(OnOkClicked);

        if (_rewardGroup != null)
            _rewardGroup.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        NetworkClient.UnregisterHandler<RedeemPromoResponse>();

        if (_redeemButton != null)
            _redeemButton.onClick.RemoveListener(OnRedeemClicked);

        if (_rewardOkButton != null)
            _rewardOkButton.onClick.RemoveListener(OnOkClicked);
    }

    private void OnRedeemClicked()
    {
        if (_feedbackText != null)
            _feedbackText.text = string.Empty;

        if (_promoInput == null)
            return;

        string code = _promoInput.text.Trim();

        if (ClientGameState.Instance == null || ClientGameState.Instance.CurrentPlayerData == null)
        {
            if (_feedbackText != null)
                _feedbackText.text = "Please log in first.";
            return;
        }

        NetworkClient.Send(new RedeemPromoRequest { code = code });
    }

    private void OnClientPromoResponse(RedeemPromoResponse resp)
    {
        if (resp.success == false)
        {
            if (_feedbackText != null)
                _feedbackText.text = resp.message;
            return;
        }

        if (ClientGameState.Instance == null)
        {
            Debug.LogWarning("[PromoUI] ClientGameState is null on promo response");
            return;
        }

        PlayerData pd = ClientGameState.Instance.CurrentPlayerData;
        if (pd == null)
        {
            if (_feedbackText != null)
                _feedbackText.text = "Player data missing";
            return;
        }

        switch (resp.rewardType)
        {
            case RewardType.Gold:
                pd.Gold += resp.amount;
                break;
            case RewardType.Diamonds:
                pd.Diamonds += resp.amount;
                break;
            default:
                Debug.LogWarning($"[PromoUI] Unknown reward type {resp.rewardType}");
                break;
        }

        PlayerStatsDisplay statsDisplay = FindObjectOfType<PlayerStatsDisplay>();
        if (statsDisplay != null)
            statsDisplay.UpdateStats();

        if (_rewardAmountText != null)
            _rewardAmountText.text = resp.amount.ToString();

        switch (resp.rewardType)
        {
            case RewardType.Gold:
                if (_rewardIcon != null) _rewardIcon.sprite = _goldSprite;
                break;
            case RewardType.Diamonds:
                if (_rewardIcon != null) _rewardIcon.sprite = _diamondsSprite;
                break;
            default:
                if (_rewardIcon != null) _rewardIcon.sprite = _tokensSprite;
                break;
        }

        if (_inputPanel != null)
            _inputPanel.SetActive(false);

        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);

        if (_rewardGroup == null)
            return;

        GameObject panelGO = _rewardGroup.gameObject;
        panelGO.SetActive(true);
        _rewardGroup.alpha = 0f;
        _rewardGroup.interactable = false;
        _rewardGroup.blocksRaycasts = false;

        StartCoroutine(FadeCanvasGroup(
            _rewardGroup, 0f, 1f, _fadeDuration,
            onComplete: () =>
            {
                _rewardGroup.interactable = true;
                _rewardGroup.blocksRaycasts = true;
            }));
    }

    private void OnOkClicked()
    {
        if (_rewardGroup == null)
            return;

        _rewardGroup.interactable = false;
        _rewardGroup.blocksRaycasts = false;

        StartCoroutine(FadeCanvasGroup(
            _rewardGroup, 1f, 0f, _fadeDuration,
            onComplete: () =>
            {
                _rewardGroup.gameObject.SetActive(false);
                if (_inputPanel != null) _inputPanel.SetActive(false);
                if (_settingsPanel != null) _settingsPanel.SetActive(false);
            }));
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup cg,
        float from,
        float to,
        float duration,
        System.Action onComplete = null)
    {
        float t = 0f;
        cg.alpha = from;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
        onComplete?.Invoke();
    }
}