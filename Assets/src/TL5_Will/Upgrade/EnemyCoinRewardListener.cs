using UnityEngine;
using EscapeThe90s.MetaProgression;

namespace EscapeThe90s.MetaProgression
{
    public class EnemyCoinRewardListener : MonoBehaviour
    {
        [SerializeField] private EnemySpawner spawner;

        private void OnEnable()
        {
            if (spawner == null)
            {
                Debug.LogError(
                    "Assign the TL4 EnemySpawner to the coin reward listener.",
                    this);
                return;
            }

            spawner.OnCoinsAwarded += HandleCoinsAwarded;
        }

        private void OnDisable()
        {
            if (spawner != null)
                spawner.OnCoinsAwarded -= HandleCoinsAwarded;
        }

        private void HandleCoinsAwarded(int amount)
        {
            var manager = MetaProgressionManager.Instance;

            if (manager == null)
            {
                Debug.LogError(
                    "MetaProgressionManager must be initialized before awarding coins.",
                    this);
                return;
            }

            manager.AddCurrency(amount);
        }
    }
}