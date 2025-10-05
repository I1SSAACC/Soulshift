using UnityEngine;
using UnityEngine.UI;

public class CustomButtonShape : MonoBehaviour
{
    [SerializeField] private float _alpha = 0.1f;

    private void Start() =>
        GetComponent<Image>().alphaHitTestMinimumThreshold = _alpha;
}
