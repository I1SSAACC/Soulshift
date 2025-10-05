using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FakeLoadingController : MonoBehaviour
{
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private float _loadDuration = 8f;
    [SerializeField] private float _postDelay = 2f;
    [SerializeField] private GameObject _objectToDisable;
    [SerializeField] private GameObject _objectToEnable;

    private void Start()
    {
        if (_objectToDisable != null)
            _objectToDisable.SetActive(true);
        if (_objectToEnable != null)
            _objectToEnable.SetActive(false);

        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        float elapsed = 0f;

        while (elapsed < _loadDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / _loadDuration);
            UpdateUI(progress);
            yield return null;
        }

        UpdateUI(1f);

        yield return new WaitForSeconds(_postDelay);

        if (_objectToDisable != null) _objectToDisable.SetActive(false);
        if (_objectToEnable != null) _objectToEnable.SetActive(true);
    }

    private void UpdateUI(float normalizedValue)
    {
        if (_loadingSlider != null)
            _loadingSlider.value = normalizedValue * 100f;

        if (_progressText != null)
        {
            int percent = Mathf.RoundToInt(normalizedValue * 100f);
            _progressText.text = $"{percent}%";
        }
    }
}