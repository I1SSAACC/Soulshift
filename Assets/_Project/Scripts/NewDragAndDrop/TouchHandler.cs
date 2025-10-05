using System;
using Unity.VisualScripting;
using UnityEngine;

public class TouchHandler : MonoBehaviour
{
    public static TouchHandler Instance { get; private set; }

    [SerializeField] private Canvas _canvas;

    private RectTransform _canvasRectTransform;
    private HeroSlot _slot;
    private Hero _hero;
    private bool _isDragging = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        _canvasRectTransform = _canvas.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_isDragging == false || _hero == null)
            return;

        Vector2 inputPosition = GetInputPosition();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            inputPosition,
            _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
            out Vector2 localPoint);

        _hero.GetComponent<RectTransform>().anchoredPosition = localPoint;

        if (IsInputEnded())
            CompleteDrag();
    }

    public void SetHeroSlotToDrag(HeroSlot slot)
    {
        if (slot == null || slot.Hero == null)
            throw new ArgumentNullException("Нет подходящего компонента для перемещения");

        _slot = slot;
        _hero = Instantiate(slot.Hero, _canvas.transform);
        _hero.SetDirection(false);
        _hero.transform.position = slot.transform.position;
        _isDragging = true;
    }

    private Vector3 GetInputPosition()
    {
        if (Application.isMobilePlatform && Input.touchCount > 0)
            return Input.GetTouch(0).position;
        else
            return Input.mousePosition;
    }

    private bool IsInputEnded()
    {
        if (Application.isMobilePlatform)
        {
            return Input.touchCount > 0 &&
                  (Input.GetTouch(0).phase == TouchPhase.Ended ||
                   Input.GetTouch(0).phase == TouchPhase.Canceled);
        }
        else
        {
            return Input.GetMouseButtonUp(0);
        }
    }

    private void CompleteDrag()
    {
        if (_hero != null)
            Destroy(_hero.gameObject);

        _slot.gameObject.SetActive(true);
        _slot = null;
        _hero = null;
        _isDragging = false;
    }
}