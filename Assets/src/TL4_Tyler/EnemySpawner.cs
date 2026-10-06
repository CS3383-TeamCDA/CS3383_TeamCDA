using System;
using UnityEngine;

public enum EnemyType
{
    Ground,
    Flying,
    Turret,
    Aim
}

public class EnemySpawner : MonoBehaviour
{
    private float spawnRate;

    public event Action<EnemyType, Vector3> OnEnemyDefeated;

    public void SetSpawnRate(float rate)
    {
        spawnRate = Mathf.Max(0.0f, rate);
        // TODO: Use this rate with TL2 chunk events and current distance for spawn timing.
    }

    public EnemyBase CreateEnemy(EnemyType type)
    {
        if (type != EnemyType.Ground && type != EnemyType.Flying && type != EnemyType.Turret && type != EnemyType.Aim)
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported enemy type.");
        }

        // TODO: Instantiate configured visual prefabs when enemy assets are available.
        GameObject enemyObject = new GameObject(type + " Enemy");
        enemyObject.transform.position = transform.position;
        EnemyBase enemy;
        switch (type)
        {
            case EnemyType.Ground:
                enemy = enemyObject.AddComponent<GroundEnemy>();
                break;
            case EnemyType.Flying:
                enemy = enemyObject.AddComponent<FlyingEnemy>();
                break;
            case EnemyType.Turret:
                enemy = enemyObject.AddComponent<TurretHazard>();
                break;
            default:
                enemy = enemyObject.AddComponent<AimEnemy>();
                break;
        }

        enemy.Type = type;
        enemy.Defeated += HandleEnemyDefeated;
        return enemy;
    }

    private void HandleEnemyDefeated(EnemyBase enemy)
    {
        enemy.Defeated -= HandleEnemyDefeated;
        if (this != null)
        {
            OnEnemyDefeated?.Invoke(enemy.Type, enemy.transform.position);
        }
    }
}
