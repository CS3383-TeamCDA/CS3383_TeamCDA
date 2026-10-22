using UnityEngine;

public class FlyingEnemy : EnemyBase
{
    public override EnemyType Type => EnemyType.Flying;
    public override EnemyMovementType MovementType => EnemyMovementType.Flying;

    public override void Move()
    {
        // TODO: Implement airborne pursuit using speed and aggroRadius.
        Debug.Log("Flying movement");
    }
}
