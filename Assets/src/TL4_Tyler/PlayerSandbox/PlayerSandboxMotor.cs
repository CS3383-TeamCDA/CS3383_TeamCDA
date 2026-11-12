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
        public bool DashJustPressed => source.DashJustPressed;
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
    public bool AirJumpAvailable => airJumpAvailable;
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
        }
        if (source.JumpJustPressed) jumpPressed = Time.time;
        State = states.Update();
        // TL6's running state doesn't transition when simply walking off a ledge.
        if (!grounded && body.linearVelocity.y <= 0 && State == "RunningState")
        {
            states.ChangeState(new JumpingState(false));
            State = "JumpingState";
        }
        if (!stance.IsLow && Time.time - jumpPressed <= BufferSeconds)
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
        if (!wasGrounded && grounded) feedback.Land();
        wasGrounded = grounded;
    }

    void FixedUpdate()
    {
        if (states == null) return;
        if (health.IsDead) { body.gravityScale = 0; body.linearVelocity = Vector2.zero; return; }
        states.FixedUpdate();
    }

    public void ResetMotor()
    {
        states.ChangeState(new RunningState());
        body.gravityScale = controller.BaseGravityScale;
        lastGrounded = jumpPressed = lastJump = float.NegativeInfinity;
        airJumpAvailable = true; wasGrounded = false;
        controller.ResetAirDash();
        State = "RunningState";
    }
}
