using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] private int startingCoins = 50;

    public int CurrentCoins { get; private set; }
    public event Action<int> OnCurrencyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentCoins = startingCoins;
    }

    private void Start()
    {
        OnCurrencyChanged?.Invoke(CurrentCoins);
    }

    public bool HasEnough(int amount) => CurrentCoins >= amount;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        CurrentCoins += amount;
        OnCurrencyChanged?.Invoke(CurrentCoins);
    }

    public bool TrySpendCoins(int cost)
    {
        if (cost < 0 || CurrentCoins < cost) return false;

        CurrentCoins -= cost;
        OnCurrencyChanged?.Invoke(CurrentCoins);
        return true;
    }
}