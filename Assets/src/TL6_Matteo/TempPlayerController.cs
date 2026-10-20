using UnityEngine;

[DefaultExecutionOrder(100)]
public class TempPlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private Collider2D _collider;
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("Horizontal")]
    [SerializeField] private float _xAcceleration = 60f;
    [SerializeField] private float _xDeceleration = 80f;
    [SerializeField] private float _maxSpeedX = 8f;

    [Header("Jump")]
    [SerializeField] private float _jumpSpeed = 10f;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.05f;

    // Give our states access to the player's components/settings.
    public Rigidbody2D Rb => _rb;
    public PlayerInputHandler Input => _inputHandler;
    public float XAcceleration => _xAcceleration;
    public float XDeceleration => _xDeceleration;
    public float MaxSpeedX => _maxSpeedX;
    public float JumpSpeed => _jumpSpeed;

    private PlayerStateManager _stateManager;

    void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_inputHandler == null)
            _inputHandler = GetComponent<PlayerInputHandler>();

        _stateManager = new PlayerStateManager(this);
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
}
