using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ButtonOpenUrl : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Кнопка, по клику на которую откроется ссылка")]
    public Button button;

    [Header("URL")]
    [Tooltip("Полный адрес (например, https://example.com)")]
    public string url;

    private void Reset()
    {
        // Попробуем автоматически найти кнопку на этом объекте
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

        // Откроет системный браузер
        Application.OpenURL(url);
    }
}