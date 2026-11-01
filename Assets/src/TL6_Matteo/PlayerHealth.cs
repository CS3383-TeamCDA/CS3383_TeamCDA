using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int _maxHealth = 100;

    public int MaxHealth => _maxHealth;
    public int CurrentHealth { get; private set; }

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    private void Awake()
    {
        CurrentHealth = _maxHealth;

        OnHealthChanged += (current, max) =>
            Debug.Log($"Player Health: {current}/{max}");

        OnPlayerDied += () =>
            Debug.Log("Player died!");
    }

    public void TakeDamage(int damage)
    {
        if(damage <= 0 || CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        if(CurrentHealth <= 0)
        {
            OnPlayerDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if(amount <= 0 || CurrentHealth <= 0) return;

        if(CurrentHealth < _maxHealth)
        {        
            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }

    }

    [ContextMenu("Test/Take 25 Damage")]
    private void TestDamage()
    {
        TakeDamage(25);
    }

    [ContextMenu("Test/Heal 25 Health")]
    private void TestHeal()
    {
        Heal(25);
    }
}