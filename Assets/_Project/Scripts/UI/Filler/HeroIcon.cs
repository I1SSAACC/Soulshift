using System;
using UnityEngine;
using UnityEngine.UI;

public class HeroIcon : MonoBehaviour
{
    [SerializeField] private Image _image;
    
    private Hero _hero;

    public Hero Hero => _hero;

    public void SetHero(Hero hero)
    {
        _hero = hero;
        SetIcon(hero.CharacterSprite);
    }

    public void SetIcon(Sprite sprite) =>
        _image.sprite = sprite;
}