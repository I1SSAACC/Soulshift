using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ButtonOpenUrl : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("������, �� ����� �� ������� ��������� ������")]
    public Button button;

    [Header("URL")]
    [Tooltip("������ ����� (��������, https://example.com)")]
    public string url;

    private void Reset()
    {
        // ��������� ������������� ����� ������ �� ���� �������
        if (!button) button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(Open);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(Open);
    }

    public void Open()
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("[ButtonOpenUrl] URL is empty");
            return;
        }

        // ������� ��������� �������
        Application.OpenURL(url);
    }
}