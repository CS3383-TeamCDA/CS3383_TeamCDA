using UnityEngine;

public class AimEnemy : EnemyBase
{
    public override EnemyType Type => EnemyType.Aim;
    public override EnemyMovementType MovementType => EnemyMovementType.Static;

    public override void Move()
    {
        // TODO: Implement aiming toward the player when target tracking is available.
        Debug.Log("Aim targeting");
    }
}
