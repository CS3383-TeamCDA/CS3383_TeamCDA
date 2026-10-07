using UnityEngine;

[RequireComponent(typeof(EnemySpawner))]
public class BindingDemo_TL4Tyler : MonoBehaviour
{
    private EnemySpawner spawner;
    private EnemyBase enemy1;
    private EnemyBase enemy2;
    private EnemyBase current;

    private void Awake()
    {
        spawner = GetComponent<EnemySpawner>();
        spawner.SetSpawnRate(5.0f);
        enemy1 = spawner.CreateEnemy(EnemyType.Ground);
        enemy1.name = "Enemy 1";
        enemy2 = spawner.CreateEnemy(EnemyType.Flying);
        enemy2.name = "Enemy 2";
        current = enemy1;
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 300, 30), current.GetType().Name);
        if (GUI.Button(new Rect(20, 100, 160, 30), "Move"))
        {
            current.Move();
        }

        if (GUI.Button(new Rect(20, 60, 160, 30), "Swap"))
        {
            current = (current == enemy1) ? enemy2 : enemy1;
        }
    }

}
