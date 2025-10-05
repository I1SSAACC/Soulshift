using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class DropPoint : MonoBehaviour
{
    private RectTransform _rectTransform;
    private DraggableItem _draggableItem;

    public RectTransform Rect => _rectTransform;

    public DraggableItem DraggableItem => _draggableItem;

    public bool CanOccupy => _draggableItem != null;

    private void Awake() =>
        _rectTransform = transform as RectTransform;

    public void Reserve(DraggableItem item)
    {
        if(item == null)
            throw new System.ArgumentNullException(nameof(item));

        _draggableItem = item;
    }

    public void Replace(DraggableItem item) =>
        _draggableItem = item;

    public void Clear() =>
        _draggableItem = null;
}