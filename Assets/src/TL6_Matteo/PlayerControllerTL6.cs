using UnityEngine;
using EscapeThe90s.Contracts;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerControllerTL6 : MonoBehaviour, IPlayerControllerTL6
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private CapsuleCollider2D _collider;
    [SerializeField] private PlayerMovementInputHandler _inputHandler;

    [Header("Horizontal")]
    [SerializeField] private float _xAcceleration = 60f;
    [SerializeField] private float _xDeceleration = 80f;
    [SerializeField] private float _defaultMaxSpeedX = 8f;

    [Header("Jump")]
    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.05f;
    [SerializeField, Min(1f)] private float _fallGravityMultiplier = 1.8f;
    [SerializeField, Min(1f)] private float _lowJumpGravityMultiplier = 2.2f;

    [Header("Dash")]
    [SerializeField] private float _dashSpeed = 16f;
    [SerializeField] private float _dashDuration = 0.2f;
    [SerializeField] private bool _disableGravityDuringDash = true;

    [Header("Crouch")]
    [SerializeField, Range(0f, 1f)]
    private float _crouchSpeedMultiplier = 0.4f;
    [SerializeField, Range(0.5f, 1f)]
    private float _crouchHeightMultiplier = 0.5f;

    private Vector2 _standingColliderSize;
    private Vector2 _standingColliderOffset;

    private float _permanentSpeedBonus = 0f;
    private float _temporarySpeedMultiplier = 1f;
    // Give our states access to the player's components/settings.
    public Rigidbody2D Rb => _rb;
    public IPlayerMovementInput Input;
    public float XAcceleration => _xAcceleration;
    public float XDeceleration => _xDeceleration;
    public float JumpSpeed => _jumpSpeed;
    public float DashSpeed => _dashSpeed;
    public float DashDuration => _dashDuration;
    public float FallGravityMultiplier => _fallGravityMultiplier;
    public float LowJumpGravityMultiplier => _lowJumpGravityMultiplier;
    public bool DisableGravityDuringDash => _disableGravityDuringDash;
    public float CrouchSpeedMultiplier => _crouchSpeedMultiplier;
    public float MaxSpeedX
    {
        get
        {
            float permanentSpeed =
                Mathf.Max(0f, _defaultMaxSpeedX + _permanentSpeedBonus);

            return permanentSpeed * _temporarySpeedMultiplier;
        }
    }

    public int FacingDirection = 1;

    public bool AirDashAvailable { get; private set; } = true;
    public bool IsDashing { get; private set; }
    public bool IsCrouching { get; private set; }
    public float BaseGravityScale { get; private set; }

    private PlayerStateManager _stateManager;



    void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_collider == null) _collider = GetComponent<CapsuleCollider2D>();
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerMovementInputHandler>();

        _stateManager = new PlayerStateManager(this);
        Input = GetComponent<IPlayerMovementInput>();
        BaseGravityScale = Rb.gravityScale;

        _standingColliderSize = _collider.size;
        _standingColliderOffset = _collider.offset;
    }

    void Start()
    {
        _stateManager.Start();
    }

    void Update()
    {
        _stateManager.Update();
    }

    void FixedUpdate()
    {
        _stateManager.FixedUpdate();
    }

    //RUNNING
    public void SetTemporarySpeedMultiplier(float multiplier)
    {
        _temporarySpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ResetTemporarySpeedMultiplier()
    {
        _temporarySpeedMultiplier = 1f;
    }

    public void SetPermanentSpeedBonus(float totalBonus)
    {
        _permanentSpeedBonus = Mathf.Max(0f, totalBonus);
    }

    //JUMPING
    public bool IsGrounded()
    {
        Bounds b = _collider.bounds;
        Vector2 center = new Vector2(
            b.center.x,
            b.min.y - _groundCheckDistance * 0.5f
        );
        Vector2 size = new Vector2(
            b.size.x * 0.9f,
            _groundCheckDistance
        );

        return Physics2D.OverlapBox(
            center, size, 0f, _groundLayer
        ) != null;
    }

    //DASHING
    public void UpdateFacingDirection(float inputX)
    {
        if (inputX > 0f)
            FacingDirection = 1;
        else if (inputX < 0f)
            FacingDirection = -1;
    }

    public void ConsumeAirDash()
    {
        AirDashAvailable = false;
    }

    public void ResetAirDash()
    {
        AirDashAvailable = true;
    }

    public void SetDashing(bool isDashing)
    {
        IsDashing = isDashing;
    }

    //CROUCHING
    public void SetCrouching(bool isCrouching)
    {
        IsCrouching = isCrouching;

        if (isCrouching)
        {
            float crouchHeight =
                _standingColliderSize.y * _crouchHeightMultiplier;

            float heightDifference =
                _standingColliderSize.y - crouchHeight;

            _collider.size = new Vector2(
                _standingColliderSize.x,
                crouchHeight
            );

            _collider.offset = new Vector2(
                _standingColliderOffset.x,
                _standingColliderOffset.y - heightDifference / 2f
            );
        }
        else
        {
            _collider.size = _standingColliderSize;
            _collider.offset = _standingColliderOffset;
        }
    }
}
