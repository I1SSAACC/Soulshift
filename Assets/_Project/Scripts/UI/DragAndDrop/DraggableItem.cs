using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private PanelSlot _originalSlot;
    [SerializeField] private DropPoint _currentPoint;
    [SerializeField] private float _snapDistance;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private CanvasGroup _canvasGroup;

    private Transform _startParent;
    private Vector3 _startWorldPos;
    private int _startSiblingIndex;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();

        if (_originalSlot == null)
        {
            if (transform.parent != null)
                _originalSlot = transform.parent.GetComponent<PanelSlot>();
        }

        _startParent = transform.parent;
        _startWorldPos = _rectTransform.position;
        _startSiblingIndex = transform.GetSiblingIndex();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //if (DragManager.Instance.IsBusy)
        //    return;

        _startParent = transform.parent;
        _startWorldPos = _rectTransform.position;
        _startSiblingIndex = transform.GetSiblingIndex();

        _canvasGroup.blocksRaycasts = false;
        transform.SetParent(_canvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform,
            eventData.position,
            _canvas.worldCamera,
            out Vector2 localPoint
        );

        _rectTransform.localPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (DragManager.Instance == null)
        {
            RestoreToPrevious();
            return;
        }

        _canvasGroup.blocksRaycasts = true;

        if (DragManager.Instance.IsBusy)
        {
            RestoreToPrevious();
            return;
        }

        DropPoint targetPoint = DragManager.Instance.FindDropPointUnderPointer(eventData);

        if (targetPoint == null)
        {
            targetPoint = DragManager.Instance.NearestPointWithinDistance(_rectTransform.position, DragManager.Instance.SnapDistance);
        }

        RectTransform panelUnderPointer = DragManager.Instance.FindPanelUnderPointer(eventData);

        if (targetPoint != null)
        {
            float dist = Vector3.Distance(_rectTransform.position, targetPoint.Rect.position);
            if (dist <= _snapDistance)
            {
                TryPlaceOnPoint(targetPoint);
                return;
            }
        }

        if (panelUnderPointer != null)
        {
            ReturnToPanel(panelUnderPointer);
            return;
        }

        RestoreToPrevious();
    }

    void TryPlaceOnPoint(DropPoint target)
    {
        if (DragManager.Instance.IsBusy) { RestoreToPrevious(); return; }

        if (target == _currentPoint)
        {
            StartCoroutine(MoveAndParent(_rectTransform.position, target.Rect.position, target.Rect, () => { }));
            return;
        }

        if (target.CanOccupy == false)
        {
            target.Reserve(this);

            var prev = _currentPoint;
            if (prev != null) prev.Clear();

            StartCoroutine(MoveAndParent(_rectTransform.position, target.Rect.position, target.Rect, () =>
            {
                target.Replace(this);
                _currentPoint = target;
            }));

            return;
        }

        var other = target.DraggableItem;

        if (other == null)
        {
            target.Reserve(this);
            StartCoroutine(MoveAndParent(_rectTransform.position, target.Rect.position, target.Rect, () =>
            {
                target.Replace(this);
                _currentPoint = target;
            }));
            return;
        }

        if (_currentPoint != null)
        {
            var myOldPoint = _currentPoint;
            target.Reserve(this);
            myOldPoint.Reserve(other);

            StartCoroutine(SwapAnimate(other, myOldPoint, this, target));

            return;
        }

        {
            target.Reserve(this);

            RectTransform destPanel = other._originalSlot != null ? other._originalSlot.Rect : DragManager.Instance.PanelRect;

            if (other._currentPoint != null)
            {
                other._currentPoint.Clear();
                other._currentPoint = null;
            }

            other.StartCoroutine(other.MoveAndParent(other._rectTransform.position, destPanel.position, destPanel, () =>
            {
                other._currentPoint = null;
            }));

            StartCoroutine(MoveAndParent(_rectTransform.position, target.Rect.position, target.Rect, () =>
            {
                target.Replace(this);
                _currentPoint = target;
            }));

            return;
        }
    }

    void ReturnToPanel(RectTransform panel)
    {
        if (DragManager.Instance.IsBusy) { RestoreToPrevious(); return; }

        if (_currentPoint != null)
        {
            _currentPoint.Clear();
            _currentPoint = null;
        }

        StartCoroutine(MoveAndParent(_rectTransform.position, panel.position, panel, () =>
        {
            if (_originalSlot != null)
            {
                transform.SetSiblingIndex(_originalSlot.transform.GetSiblingIndex());
            }
        }));
    }

    void RestoreToPrevious()
    {
        if (_currentPoint != null)
        {
            StartCoroutine(MoveAndParent(_rectTransform.position, _currentPoint.Rect.position, _currentPoint.Rect, null));
            return;
        }

        if (_originalSlot != null)
        {
            StartCoroutine(MoveAndParent(_rectTransform.position, _originalSlot.Rect.position, _originalSlot.Rect, null));
            return;
        }

        StartCoroutine(MoveAndParent(_rectTransform.position, _startWorldPos, _startParent as RectTransform, () =>
        {
            transform.SetSiblingIndex(_startSiblingIndex);
        }));
    }

    private IEnumerator MoveAndParent(Vector3 from, Vector3 to, RectTransform finalParent, Action onComplete = null)
    {
        DragManager.Instance.SetBusyStatus(true);

        float t = 0f;
        float dur = Mathf.Max(0.0001f, DragManager.Instance.MoveDuration);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            _rectTransform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.SetParent(finalParent, false);
        _rectTransform.anchoredPosition = Vector2.zero;

        onComplete?.Invoke();

        DragManager.Instance.SetBusyStatus(false);
    }

    private IEnumerator SwapAnimate(DraggableItem a, DropPoint aDest, DraggableItem b, DropPoint bDest)
    {
        DragManager.Instance.SetBusyStatus(true);

        Vector3 aFrom = a._rectTransform.position;
        Vector3 aTo = aDest.Rect.position;
        Vector3 bFrom = b._rectTransform.position;
        Vector3 bTo = bDest.Rect.position;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, DragManager.Instance.MoveDuration);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            a._rectTransform.position = Vector3.Lerp(aFrom, aTo, Mathf.SmoothStep(0f, 1f, t));
            b._rectTransform.position = Vector3.Lerp(bFrom, bTo, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        a.transform.SetParent(aDest.Rect, false);
        a._rectTransform.anchoredPosition = Vector2.zero;
        b.transform.SetParent(bDest.Rect, false);
        b._rectTransform.anchoredPosition = Vector2.zero;

        aDest.Replace(a);
        bDest.Replace(b);

        a._currentPoint = aDest;
        b._currentPoint = bDest;

        DragManager.Instance.SetBusyStatus(false);

        yield break;
    }
}