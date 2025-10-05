using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroSlot : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    private const float _dragThreshold = 10f;

    [SerializeField] private Image _character;
    [SerializeField] private Image _frame;
    [SerializeField] private Image _levelShield;
    [SerializeField] private Image _gradient;
    [SerializeField] private TextMeshProUGUI _level;
    [SerializeField] private Stars _stars;

    private Hero _hero;
    private ScrollRect _scrollRect;
    private Vector2 _startDragPosition;
    private bool _isDragging;
    private bool _isPlayerSlot;
    private bool _isScrollDrag;

    public Hero Hero => _hero;

    private void Awake() =>
        _scrollRect = GetComponentInParent<ScrollRect>();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isPlayerSlot == false)
            return;

        _startDragPosition = eventData.position;
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDragging == false)
            return;

        Vector2 dragDelta = eventData.position - _startDragPosition;

        if (Mathf.Abs(dragDelta.y) > Mathf.Abs(dragDelta.x) &&
            Mathf.Abs(dragDelta.y) > _dragThreshold)
        {
            TouchHandler.Instance.SetHeroSlotToDrag(this);
            gameObject.SetActive(false);
            _isDragging = false;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    public void SetHero(Hero hero, bool isLookLeft = false)
    {
        _hero = hero;
        UpdateView(isLookLeft);
    }

    public void SetSlotStatus(bool isPlayerSlot) =>
        _isPlayerSlot = isPlayerSlot;

    private void UpdateView(bool isLookLeft)
    {
        if (_hero == null)
            return;

        Vector3 scale = _character.transform.localScale;
        scale.x = isLookLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        _character.transform.localScale = scale;

        _character.sprite = _hero.CharacterSprite;
        //_frame.sprite = _hero.FrameSprite;
        _levelShield.sprite = _hero.LevelShieldSprite;
        _gradient.color = _hero.GradientColor;
        _level.text = _hero.Level.ToString();
        _stars.ActivateStars(_hero.StarsCount);
    }
}