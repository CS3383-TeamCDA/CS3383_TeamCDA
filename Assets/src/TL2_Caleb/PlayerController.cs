using UnityEngine;

public class PlayerController : MonoBehaviour
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

    void Awake()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_collider == null) _collider = GetComponent<Collider2D>();
        if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        // WasPressedThisFrame lasts one rendered frame
        if (_inputHandler.JumpJustPressed && IsGrounded())
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpSpeed);
        }
    }

    void FixedUpdate()
    {
        float inputX = Mathf.Clamp(_inputHandler.Movement.x, -1f, 1f);
        float targetSpeed = inputX * _maxSpeedX;
        float rate = Mathf.Approximately(inputX, 0f) ? _xDeceleration : _xAcceleration;

        float newX = Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);
    }

    bool IsGrounded()
    {
        Bounds b = _collider.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - _groundCheckDistance * 0.5f);
        Vector2 size = new Vector2(b.size.x * 0.9f, _groundCheckDistance);
        return Physics2D.OverlapBox(center, size, 0f, _groundLayer);
    }
}