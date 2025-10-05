using UnityEngine;

public class StarActivator : MonoBehaviour
{
    [SerializeField] private GameObject _star;

    public void Activate() =>
        _star.SetActive(true);

    public void Deactivate() =>
        _star.SetActive(false);
}