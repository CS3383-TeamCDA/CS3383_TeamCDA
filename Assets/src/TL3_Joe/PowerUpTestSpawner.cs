using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    public class PowerUpTestSpawner : MonoBehaviour
    {
        [SerializeField] private PowerUpManager manager;
        [SerializeField] private PowerUpType type = PowerUpType.Shield;
        [SerializeField] private Vector3 position = new Vector3(5f, 1f, 0f);

        private void Start()
        {
            manager.SpawnPowerUp(position, type);
        }
    }
}
