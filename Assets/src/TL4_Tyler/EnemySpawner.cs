using UnityEngine;

public enum EnemyType
{
    Basic
}

public class EnemySpawner : MonoBehaviour
{
    private float spawnRate;

    public void SetSpawnRate(float newSpawnRate)
    {
        spawnRate = newSpawnRate;
    }

    public EnemyBase CreateEnemy(EnemyType type)
    {
        if (type != EnemyType.Basic)
        {
            throw new System.ArgumentOutOfRangeException(nameof(type), type, "Unsupported enemy type.");
        }

        GameObject enemyObject = new GameObject("Basic Enemy");
        enemyObject.transform.position = transform.position;
        Debug.Log("Enemy created");
        return enemyObject.AddComponent<EnemyBase>();
    }
}
