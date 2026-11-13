using System;

namespace EscapeThe90s.Contracts
{
    public interface IMetaProgressionManager
    {
        // Read-only queries (no side effects)
        int Currency { get; }                          // needed by the shop UI to show the balance
        int GetUpgradeLevel(UpgradeType type);         // 0 if unknown
        float GetEffectValue(UpgradeType type);         // 0 if unknown

        event Action<int> OnCurrencyChanged;

        // Commands
        void AddCurrency(int amount);                   

    }
}