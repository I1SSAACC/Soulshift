using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class SettingsManager : MonoBehaviour
{
    // PlayerPrefs keys
    const string KEY_MUSIC = "settings_music_volume";
    const string KEY_SFX = "settings_sfx_volume";
    const string KEY_VIBRATION = "settings_vibration";
    const string KEY_NOTIFICATIONS = "settings_notifications";

    [Header("UI")]
    [SerializeField] Slider _musicSlider;
    [SerializeField] TMP_Text _musicValueText;
    [SerializeField] Slider _sfxSlider;
    [SerializeField] TMP_Text _sfxValueText;
    [SerializeField] Toggle _vibrationToggle;
    [SerializeField] Toggle _notificationsToggle;

    [Header("Colors")]
    [SerializeField] Color _textColorIdle = Color.white;
    [SerializeField] Color _textColorActive = Color.cyan;

    [Header("Audio")]
    [SerializeField] AudioSource _musicAudioSource;
    [SerializeField] AudioSource _sfxAudioSource;

    void Awake()
    {
        LoadPrefs();
        BindUiCallbacks();
        EnsureEventTriggers();
        RefreshUi();
        ApplyAudioVolumes();
    }

    void LoadPrefs()
    {
        float music = PlayerPrefs.HasKey(KEY_MUSIC) ? PlayerPrefs.GetFloat(KEY_MUSIC) : 1f;
        float sfx = PlayerPrefs.HasKey(KEY_SFX) ? PlayerPrefs.GetFloat(KEY_SFX) : 1f;
        bool vibration = PlayerPrefs.HasKey(KEY_VIBRATION) ? PlayerPrefs.GetInt(KEY_VIBRATION) == 1 : true;
        bool notifications = PlayerPrefs.HasKey(KEY_NOTIFICATIONS) ? PlayerPrefs.GetInt(KEY_NOTIFICATIONS) == 1 : true;

        if (_musicSlider != null) _musicSlider.value = music;
        if (_sfxSlider != null) _sfxSlider.value = sfx;
        if (_vibrationToggle != null) _vibrationToggle.isOn = vibration;
        if (_notificationsToggle != null) _notificationsToggle.isOn = notifications;
    }

    void BindUiCallbacks()
    {
        if (_musicSlider != null)
        {
            _musicSlider.onValueChanged.RemoveAllListeners();
            _musicSlider.onValueChanged.AddListener(OnMusicSliderValueChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.onValueChanged.RemoveAllListeners();
            _sfxSlider.onValueChanged.AddListener(OnSfxSliderValueChanged);
        }

        if (_vibrationToggle != null)
        {
            _vibrationToggle.onValueChanged.RemoveAllListeners();
            _vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
        }

        if (_notificationsToggle != null)
        {
            _notificationsToggle.onValueChanged.RemoveAllListeners();
            _notificationsToggle.onValueChanged.AddListener(OnNotificationsToggleChanged);
        }
    }

    void EnsureEventTriggers()
    {
        // Add PointerDown/PointerUp handlers to sliders so text color changes automatically.
        AddPointerEventsToSlider(_musicSlider, OnMusicPointerDown, OnMusicPointerUp);
        AddPointerEventsToSlider(_sfxSlider, OnSfxPointerDown, OnSfxPointerUp);
    }

    void AddPointerEventsToSlider(Slider slider, UnityEngine.Events.UnityAction<BaseEventData> onDown, UnityEngine.Events.UnityAction<BaseEventData> onUp)
    {
        if (slider == null) return;

        var go = slider.gameObject;
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        trigger.triggers = trigger.triggers ?? new System.Collections.Generic.List<EventTrigger.Entry>();

        // PointerDown
        var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback = new EventTrigger.TriggerEvent();
        downEntry.callback.AddListener(onDown);
        trigger.triggers.Add(downEntry);

        // PointerUp
        var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback = new EventTrigger.TriggerEvent();
        upEntry.callback.AddListener(onUp);
        trigger.triggers.Add(upEntry);
    }

    void RefreshUi()
    {
        UpdateMusicText(_musicSlider != null ? _musicSlider.value : 0f);
        UpdateSfxText(_sfxSlider != null ? _sfxSlider.value : 0f);
        SetMusicTextColor(_textColorIdle);
        SetSfxTextColor(_textColorIdle);
    }

    void ApplyAudioVolumes()
    {
        if (_musicAudioSource != null && _musicSlider != null) _musicAudioSource.volume = _musicSlider.value;
        if (_sfxAudioSource != null && _sfxSlider != null) _sfxAudioSource.volume = _sfxSlider.value;
    }

    // UI callbacks
    void OnMusicSliderValueChanged(float value)
    {
        UpdateMusicText(value);
        if (_musicAudioSource != null) _musicAudioSource.volume = value;
        PlayerPrefs.SetFloat(KEY_MUSIC, value);
    }

    void OnSfxSliderValueChanged(float value)
    {
        UpdateSfxText(value);
        if (_sfxAudioSource != null) _sfxAudioSource.volume = value;
        PlayerPrefs.SetFloat(KEY_SFX, value);
    }

    void OnVibrationToggleChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_VIBRATION, on ? 1 : 0);
    }

    void OnNotificationsToggleChanged(bool on)
    {
        PlayerPrefs.SetInt(KEY_NOTIFICATIONS, on ? 1 : 0);
    }

    // Pointer handlers for color change
    void OnMusicPointerDown(BaseEventData _)
    {
        SetMusicTextColor(_textColorActive);
    }

    void OnMusicPointerUp(BaseEventData _)
    {
        SetMusicTextColor(_textColorIdle);
        if (_musicSlider != null) PlayerPrefs.SetFloat(KEY_MUSIC, _musicSlider.value);
    }

    void OnSfxPointerDown(BaseEventData _)
    {
        SetSfxTextColor(_textColorActive);
    }

    void OnSfxPointerUp(BaseEventData _)
    {
        SetSfxTextColor(_textColorIdle);
        if (_sfxSlider != null) PlayerPrefs.SetFloat(KEY_SFX, _sfxSlider.value);
    }

    // Helpers
    void UpdateMusicText(float normalized)
    {
        if (_musicValueText == null) return;
        int value = Mathf.RoundToInt(normalized * 100f);
        _musicValueText.text = value.ToString();
    }

    void UpdateSfxText(float normalized)
    {
        if (_sfxValueText == null) return;
        int value = Mathf.RoundToInt(normalized * 100f);
        _sfxValueText.text = value.ToString();
    }

    void SetMusicTextColor(Color c)
    {
        if (_musicValueText == null) return;
        _musicValueText.color = c;
    }

    void SetSfxTextColor(Color c)
    {
        if (_sfxValueText == null) return;
        _sfxValueText.color = c;
    }

    // Public API for forcing save
    public void SaveNow()
    {
        if (_musicSlider != null) PlayerPrefs.SetFloat(KEY_MUSIC, _musicSlider.value);
        if (_sfxSlider != null) PlayerPrefs.SetFloat(KEY_SFX, _sfxSlider.value);
        if (_vibrationToggle != null) PlayerPrefs.SetInt(KEY_VIBRATION, _vibrationToggle.isOn ? 1 : 0);
        if (_notificationsToggle != null) PlayerPrefs.SetInt(KEY_NOTIFICATIONS, _notificationsToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnApplicationQuit()
    {
        SaveNow();
    }
}