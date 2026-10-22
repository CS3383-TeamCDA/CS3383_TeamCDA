using UnityEngine;

// Place on a room prefab at an intentional, valid enemy origin (feet for the ground prefab).
public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private EnemyType enemyType = EnemyType.Ground;
    [SerializeField] private EnemyBase enemyPrefab;
    [SerializeField] private Vector2 clearanceSize = new Vector2(2, 2);
    [SerializeField] private Vector2 clearanceOffset = new Vector2(0, 1);
    [SerializeField] private LayerMask blockingLayers;
    public EnemyType Type => enemyType;
    public EnemyBase Prefab => enemyPrefab;
    public Bounds ClearanceBounds => new Bounds(transform.position + (Vector3)clearanceOffset,
        new Vector3(Mathf.Max(.1f, clearanceSize.x), Mathf.Max(.1f, clearanceSize.y), .1f));

    public bool IsAvailable
    {
        get
        {
            if (!isActiveAndEnabled || (enemyPrefab != null && enemyPrefab.Type != enemyType)) return false;
            var bounds = ClearanceBounds;
            return blockingLayers.value == 0 || Physics2D.OverlapBox(bounds.center, bounds.size, 0, blockingLayers) == null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(ClearanceBounds.center, ClearanceBounds.size);
    }
}
