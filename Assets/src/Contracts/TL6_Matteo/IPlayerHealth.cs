using System;
using UnityEngine;

namespace EscapeThe90s.Contracts
{
    public interface IPlayerHealth
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        event Action<int, int> OnHealthChanged; // {Current, Max}
        event Action OnPlayerDied;

        public void TakeDamage(int damage);
        public void Heal(int amount);
        public void ResetHealth();

        //REMOVE AFTER PERMANENT UPGRADE IS SWITCHED TO NEW INTERFACE
        public void IncreaseMaxHealth(int amount);
    }
}
