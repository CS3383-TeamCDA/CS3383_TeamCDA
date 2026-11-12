using UnityEngine;

/// <summary>Sandbox jump extensions around TL6's existing movement states.</summary>
[DefaultExecutionOrder(50)]
public sealed class PlayerSandboxMotor : MonoBehaviour
{
    sealed class MotorInput : IPlayerMovementInput
    {
        public IPlayerMovementInput source;
        public Vector2 Movement => source.Movement;
        // This harness owns jump impulses; TL6 still owns horizontal motion and dash.
        public bool JumpJustPressed => false;
        public bool JumpBeingHeld => source.JumpBeingHeld;
        public bool JumpReleased => source.JumpReleased;
        public bool DashJustPressed => false;
    }
    const float CoyoteSeconds = .12f, BufferSeconds = .14f;
    PlayerControllerTL6 controller;
    PlayerStateManager states;
    PlayerCrouchSlide stance;
    PlayerHealth health;
    PlayerSandboxFeedback feedback;
    IPlayerMovementInput source;
    Rigidbody2D body;
    float lastGrounded = float.NegativeInfinity;
    float jumpPressed = float.NegativeInfinity;
    float lastJump = float.NegativeInfinity;
    bool airJumpAvailable = true, wasGrounded;
    bool dashConsumedAir;
    float nextDash;
    public bool AirJumpAvailable => airJumpAvailable;
    public bool IsDashing => health != null && !health.IsDead && State == "DashingState";
    public int DashDirection { get; private set; } = 1;
    public string State { get; private set; }

    public void Initialize(PlayerControllerTL6 player, PlayerCrouchSlide lowStance, PlayerSandboxFeedback visuals)
    {
        controller = player; stance = lowStance; feedback = visuals;
        body = controller.Rb;
        health = GetComponent<PlayerHealth>();
        source = controller.Input;
        controller.Input = new MotorInput { source = source };
        // Own one TL6 state manager rather than running two physics controllers.
        controller.enabled = false;
        states = new PlayerStateManager(controller);
        states.Start();
        State = "RunningState";
    }

    void Update()
    {
        if (states == null || health.IsDead) return;
        bool grounded = controller.IsGrounded() && body.linearVelocity.y <= .1f && Time.time - lastJump > .06f;
        if (grounded)
        {
            lastGrounded = Time.time;
            airJumpAvailable = true;
            controller.ResetAirDash();
            dashConsumedAir = false;
        }
        if (source.JumpJustPressed) jumpPressed = Time.time;
        State = states.Update();
        // TL6's running state doesn't transition when simply walking off a ledge.
        if (!grounded && body.linearVelocity.y <= 0 && State == "RunningState")
        {
            states.ChangeState(new JumpingState(false));
            State = "JumpingState";
        }
        if (!stance.IsLow && Time.time - jumpPressed <= BufferSeconds && !source.DashJustPressed)
        {
            bool groundJump = grounded || Time.time - lastGrounded <= CoyoteSeconds;
            bool extraJump = !groundJump && airJumpAvailable && source.JumpJustPressed && Time.time - lastJump > .08f;
            if (groundJump || extraJump)
            {
                states.ChangeState(new JumpingState(false));
                State = "JumpingState";
                body.gravityScale = controller.BaseGravityScale;
                body.linearVelocity = new Vector2(body.linearVelocity.x, controller.JumpSpeed);
                lastJump = Time.time;
                lastGrounded = jumpPressed = float.NegativeInfinity;
                if (extraJump)
                {
                    airJumpAvailable = false;
                    feedback.BeginFlip(controller.FacingDirection);
                }
            }
        }
        if (!stance.IsLow && source.DashJustPressed && !IsDashing && Time.time >= nextDash &&
            (grounded || controller.AirDashAvailable))
        {
            float move = source.Movement.x;
            DashDirection = Mathf.Abs(move) > .1f ? (move > 0 ? 1 : -1) : controller.FacingDirection;
            if (DashDirection == 0) DashDirection = 1;
            controller.FacingDirection = DashDirection;
            dashConsumedAir = !grounded;
            if (dashConsumedAir) controller.ConsumeAirDash();
            states.ChangeState(new DashingState());
            State = "DashingState";
            nextDash = Time.time + controller.DashDuration + .12f;
            feedback.BeginDash();
            GetComponent<PlayerMotionEffects>().DashBurst(DashDirection);
        }
        // A ground dash that leaves a ledge also spends this airtime's dash.
        if (IsDashing && !grounded && !dashConsumedAir)
        {
            controller.ConsumeAirDash();
            dashConsumedAir = true;
        }
        feedback.SetDashing(IsDashing);
        if (!wasGrounded && grounded) feedback.Land();
        wasGrounded = grounded;
    }

    void FixedUpdate()
    {
        if (states == null) return;
        if (health.IsDead) return; // Death feedback owns the short physical flop.
        states.FixedUpdate();
    }

    public void ResetMotor()
    {
        states.ChangeState(new RunningState());
        body.gravityScale = controller.BaseGravityScale;
        lastGrounded = jumpPressed = lastJump = float.NegativeInfinity;
        airJumpAvailable = true; wasGrounded = false;
        dashConsumedAir = false; nextDash = 0;
        feedback.SetDashing(false);
        controller.ResetAirDash();
        State = "RunningState";
    }
}
