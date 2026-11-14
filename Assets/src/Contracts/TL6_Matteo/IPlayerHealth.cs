using UnityEngine;

namespace EscapeThe90s.Contracts
{
    public interface IPlayerHealth
    {
        public void TakeDamage(int damage);
        public void Heal(int amount);
        public void ResetHealth();
        public void IncreaseMaxHealth(int amount);
        public void ResetMaxHealth();
    }
}
