using System.Collections.Generic;
using UnityEngine;

public class HeroFiller : MonoBehaviour
{
    [SerializeField] private Hero[] _availableHeroes;
    [SerializeField] private HeroSpawnPoint[] _spawnPoints;
    [SerializeField] private HeroSlot _heroSlotPrefab;
    [SerializeField] private Transform _content;
    [SerializeField] private int _count = 3;

    private void Awake() =>
        Arrange();

    private void Arrange()
    {
        List<Hero> tempHeroes = new(_availableHeroes);

        for (int i = 0; i < _count; i++)
        {
            Hero hero = tempHeroes[Random.Range(0, tempHeroes.Count)];
            tempHeroes.Remove(hero);

            HeroSlot slot = Instantiate(_heroSlotPrefab, _content);
            slot.SetHero(hero, true);

            Hero heroInstance = Instantiate(hero, _spawnPoints[i].transform);
            heroInstance.SetDirection(true);
        }
    }
}