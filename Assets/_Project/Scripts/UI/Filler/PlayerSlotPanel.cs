using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotPanel : MonoBehaviour
{
    [SerializeField] private Hero[] _availableHeroes;
    [SerializeField] private HeroSlot _playerSlotPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private ScrollRect _scrollRect;

    private readonly List<HeroSlot> _playerSlots = new();

    private void Awake()
    {
        foreach (Transform child in _content)
            Destroy(child.gameObject);

        foreach (Hero hero in _availableHeroes)
            InstantiateSlot(hero);

        // Ќастройте скролл дл€ горизонтальной прокрутки
        if (_scrollRect != null)
        {
            _scrollRect.vertical = false;
            _scrollRect.horizontal = true;
        }
    }

    private void InstantiateSlot(Hero hero)
    {
        HeroSlot playerSlot = Instantiate(_playerSlotPrefab, _content);
        playerSlot.SetHero(hero);
        playerSlot.SetSlotStatus(true);
        _playerSlots.Add(playerSlot);
    }
}