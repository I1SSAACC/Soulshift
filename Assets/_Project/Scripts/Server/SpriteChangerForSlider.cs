using UnityEngine;
using UnityEngine.UI;

public class SpriteChangerForSlider : MonoBehaviour
{
    [SerializeField] private Sprite _orangeSprite;
    [SerializeField] private Sprite _yellowSprite;
    [SerializeField] private Slider _slider;
    [SerializeField] private Image _image;

    private void OnEnable() =>
        _slider.onValueChanged.AddListener(OnChanged);
    
    private void OnDisable() =>
        _slider.onValueChanged.RemoveListener(OnChanged);

    private void OnChanged(float value)
    {
        Sprite targetSprite = Mathf.Approximately(value, 1f)
            ? _yellowSprite
            : _orangeSprite;

        if (_image.sprite == targetSprite)
            return;

        _image.sprite = targetSprite;
    }
}