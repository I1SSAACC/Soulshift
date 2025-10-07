using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class ToggleReplaceObjects : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] GameObject _targetOn;
    [SerializeField] GameObject _targetOff;

    Toggle _toggle;

    void Reset()
    {
        _toggle = GetComponent<Toggle>();
    }

    void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    void OnEnable()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(HandleToggle);
            _toggle.onValueChanged.AddListener(HandleToggle);
            ApplyState(_toggle.isOn);
        }
        else
        {
            ApplyState(false);
        }
    }

    void OnDisable()
    {
        if (_toggle != null)
            _toggle.onValueChanged.RemoveListener(HandleToggle);
    }

    void HandleToggle(bool isOn)
    {
        ApplyState(isOn);
    }

    void ApplyState(bool isOn)
    {
        if (_targetOn != null) _targetOn.SetActive(isOn);
        if (_targetOff != null) _targetOff.SetActive(!isOn);
    }

    // Public API
    public void SetTargets(GameObject onObj, GameObject offObj)
    {
        _targetOn = onObj;
        _targetOff = offObj;
        if (isActiveAndEnabled)
            ApplyState(_toggle != null && _toggle.isOn);
    }
}