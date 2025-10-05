using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragManager : MonoBehaviour
{
    public static DragManager Instance { get; private set; }

    [SerializeField] private List<DropPoint> _dropPoints = new();

    private bool _isBusy;

    public RectTransform PanelRect;
    public float MoveDuration;
    public float SnapDistance;

    public bool IsBusy => _isBusy;


    private void Awake()
    {
        if (Instance == null) 
            Instance = this;

        else Destroy(gameObject);
    }

    public DropPoint FindDropPointUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        DropPoint found = null;
        foreach (var r in results)
        {
            if (r.gameObject == eventData.pointerDrag) continue;
            var dp = r.gameObject.GetComponentInParent<DropPoint>();
            if (dp != null) { found = dp; break; }
        }
        return found;
    }

    public RectTransform FindPanelUnderPointer(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            var layout = r.gameObject.GetComponentInParent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null) return layout.transform as RectTransform;
        }
        return null;
    }

    public DropPoint NearestPointWithinDistance(Vector3 worldPos, float maxDistance)
    {
        DropPoint best = null;
        float bestDist = maxDistance;
        foreach (var p in _dropPoints)
        {
            float d = Vector3.Distance(worldPos, p.Rect.position);
            if (d <= bestDist)
            {
                bestDist = d;
                best = p;
            }
        }
        return best;
    }

    public void SetBusyStatus(bool isBusy)
    {
        if (_isBusy == isBusy) 
            return;

        _isBusy = isBusy;
    }
}