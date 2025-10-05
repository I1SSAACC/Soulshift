using UnityEngine;

public class Stars : MonoBehaviour
{
    [SerializeField] private StarActivator[] _starActivators;

    public void ActivateStars(int count)
    {
        for (int i = 0; i < _starActivators.Length; i++)
        {
            if (i < count)
                _starActivators[i].Activate();
            else
                _starActivators[i].Deactivate();
        }
    }
}