using UnityEngine;

public class Hero : MonoBehaviour
{
    [SerializeField] private Sprite _character;
    [SerializeField] private Sprite _frame;
    [SerializeField] private Sprite _levelShield;
    [SerializeField] private Color _gradient;
    [SerializeField] private int _starsCount;
    [SerializeField] private int _level;
    [SerializeField] private float _damage;
    [SerializeField] private float _defense;
    [SerializeField] private float _health;

    public Sprite CharacterSprite => _character;

    public Sprite FrameSprite => _frame;

    public Sprite LevelShieldSprite => _levelShield;

    public Color GradientColor => _gradient;

    public int Level => _level;

    public int StarsCount => _starsCount;

    public void SetDirection(bool isLookLeft)
    {
        Vector3 scale = transform.localScale;
        scale.x = isLookLeft ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}