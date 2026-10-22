using UnityEngine;

public class GroundEnemy : EnemyBase
{
    public override EnemyType Type => EnemyType.Ground;
    public override EnemyMovementType MovementType => EnemyMovementType.Ground;

    [SerializeField] private bool facingRight = true;

    // Read the actual displayed facing, including a mirrored parent transform.
    public bool FacingRight
    {
        get
        {
            var sprite = GetComponent<SpriteRenderer>();
            return sprite != null ? !sprite.flipX == (transform.lossyScale.x >= 0) : facingRight;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        SetFacingRight(facingRight);
    }

    public void SetFacingRight(bool right)
    {
        facingRight = right;
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.flipX = right != (transform.lossyScale.x >= 0);
    }

    protected override bool CalculateShotDirection(Vector2 origin, Vector2 target, out Vector2 direction)
    {
        direction = FacingRight ? Vector2.right : Vector2.left;
        return true;
    }

    public override void Move()
    {
        // TODO: Implement ground pursuit using speed and aggroRadius.
        Debug.Log("Ground movement");
    }
}
