using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PanelSlot : MonoBehaviour
{
    public RectTransform Rect { get; private set; }

    void Awake() =>
        Rect = GetComponent<RectTransform>();
}