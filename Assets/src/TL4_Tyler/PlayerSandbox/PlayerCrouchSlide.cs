using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>TL4 stance overlay; forwards normal movement to the unmodified TL6 controller.</summary>
[DefaultExecutionOrder(200)]
public sealed class PlayerCrouchSlide : MonoBehaviour
{
    sealed class StanceInput : IPlayerMovementInput
    {
        public PlayerCrouchSlide owner;
        public IPlayerMovementInput source;
        bool BlockActions => owner.IsLow || Held();
        public Vector2 Movement => BlockActions ? Vector2.zero : source.Movement;
        public bool JumpJustPressed => !BlockActions && source.JumpJustPressed;
        public bool JumpBeingHeld => !BlockActions && source.JumpBeingHeld;
        public bool JumpReleased => source.JumpReleased;
        public bool DashJustPressed => !BlockActions && source.DashJustPressed;
    }
    PlayerControllerTL6 controller;
    Rigidbody2D body;
    CapsuleCollider2D capsule;
    PlayerHealth health;
    StanceInput input;
    Vector2 standingSize, standingOffset;
    float slideSpeed, slideTime;
    bool wasHeld;
    public bool IsLow { get; private set; }
    public bool IsSliding { get; private set; }

    static bool Held()
    {
        var k = Keyboard.current;
        return k != null && (k.sKey.isPressed || k.downArrowKey.isPressed || k.leftCtrlKey.isPressed);
    }

    public void Initialize(PlayerControllerTL6 player)
    {
        controller = player;
        body = player.Rb;
        health = GetComponent<PlayerHealth>();
        capsule = GetComponent<CapsuleCollider2D>();
        standingSize = capsule.size;
        standingOffset = capsule.offset;
        input = new StanceInput { owner = this, source = controller.Input };
        controller.Input = input;
    }

    void Update()
    {
        if (controller == null || health.IsDead) return;
        bool held = Held();
        bool grounded = controller.IsGrounded() && body.linearVelocity.y <= .1f;
        if (held && grounded && !IsLow)
        {
            SetLow(true);
            if (!wasHeld && Mathf.Abs(body.linearVelocity.x) >= 4f)
            {
                IsSliding = true;
                slideSpeed = Mathf.Sign(body.linearVelocity.x) * Mathf.Max(10f, Mathf.Abs(body.linearVelocity.x));
                slideTime = .65f;
            }
        }
        if (IsSliding && (!grounded || !held || slideTime <= 0 || Mathf.Abs(slideSpeed) < 2.5f)) IsSliding = false;
        if (IsLow && !held && !IsSliding && CanStand()) SetLow(false);
        wasHeld = held;
    }

    void FixedUpdate()
    {
        if (controller == null || health.IsDead || !IsLow) return;
        if (IsSliding)
        {
            slideTime -= Time.fixedDeltaTime;
            slideSpeed = Mathf.MoveTowards(slideSpeed, 0, 16f * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(slideSpeed, body.linearVelocity.y);
        }
        else
        {
            float x = input.source.Movement.x;
            controller.UpdateFacingDirection(x);
            body.linearVelocity = new Vector2(x * 2.5f, body.linearVelocity.y);
        }
    }

    bool CanStand()
    {
        // Test only the space above the shortened capsule, inset from supporting surfaces.
        float top = transform.position.y + standingOffset.y + standingSize.y * .5f;
        float lowTop = capsule.bounds.max.y;
        float height = top - lowTop;
        if (height <= .02f) return true;
        var hit = Physics2D.OverlapBox(new Vector2(capsule.bounds.center.x, lowTop + height * .5f + .01f),
            new Vector2(standingSize.x * .9f, height - .01f), 0, LayerMask.GetMask("Ground"));
        return hit == null;
    }

    void SetLow(bool low)
    {
        IsLow = low;
        float height = standingSize.y * .65f;
        capsule.size = low ? new Vector2(standingSize.x, height) : standingSize;
        capsule.offset = low ? standingOffset + Vector2.down * ((standingSize.y - height) * .5f) : standingOffset;
    }

    public void ResetStance()
    {
        IsSliding = false;
        slideSpeed = slideTime = 0;
        wasHeld = true;
        SetLow(false);
    }
}
