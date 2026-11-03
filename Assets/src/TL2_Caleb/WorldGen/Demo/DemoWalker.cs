using UnityEngine;

// DEMO ONLY. A throwaway player so the generator can be showcased without the team's real player.
// Delete the whole Demo folder when the real player exists; nothing in the generator depends on it.
// Controls: A/D or arrow keys to move, Space to jump. Works with the old and the new Input System.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class DemoWalker : MonoBehaviour
{
    public float moveSpeed = 7f;        // units per second (16 px per unit)
    public float jumpVelocity = 14f;
    public float gravityScale = 3f;

    Rigidbody2D _rb;
    readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];
    float _horizontal;
    bool _jumpQueued;

    Vector2 Velocity
    {
        get
        {
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity;
#else
            return _rb.velocity;
#endif
        }
        set
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = value;
#else
            _rb.velocity = value;
#endif
        }
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = gravityScale;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var capsule = GetComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.75f, 1.25f);        // 12 x 20 px
        capsule.offset = new Vector2(0f, 0.625f);        // pivot at the feet
        capsule.sharedMaterial = new PhysicsMaterial2D("DemoWalkerNoFriction") { friction = 0f, bounciness = 0f };

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSprite();
        sr.sortingOrder = 5;
        Shader unlit = Shader.Find("Sprites/Default");   // unlit, so it shows with or without 2D lights
        if (unlit != null) sr.sharedMaterial = new Material(unlit);
    }

    void Update()
    {
        _horizontal = ReadHorizontal();
        if (JumpPressed()) _jumpQueued = true;
    }

    void FixedUpdate()
    {
        Vector2 v = Velocity;
        v.x = _horizontal * moveSpeed;
        if (_jumpQueued && IsGrounded()) v.y = jumpVelocity;
        _jumpQueued = false;
        Velocity = v;
    }

    bool IsGrounded()
    {
        int n = _rb.GetContacts(_contacts);
        for (int i = 0; i < n; i++)
            if (_contacts[i].normal.y > 0.5f) return true;
        return false;
    }

    static float ReadHorizontal()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        if (k == null) return 0f;
        float h = 0f;
        if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
        if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
        return h;
#else
        return Input.GetAxisRaw("Horizontal");
#endif
    }

    static bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = UnityEngine.InputSystem.Keyboard.current;
        return k != null && k.spaceKey.wasPressedThisFrame;
#else
        return Input.GetButtonDown("Jump");
#endif
    }

    static Sprite MakeSprite()
    {
        const int w = 12, h = 20;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var px = new Color32[w * h];
        var body = new Color32(90, 210, 255, 255);
        var dark = new Color32(15, 20, 50, 255);
        var white = new Color32(255, 255, 255, 255);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool edge = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                px[y * w + x] = edge ? dark : body;
            }
        px[14 * w + 3] = white; px[14 * w + 8] = white;   // eyes
        px[13 * w + 3] = dark;  px[13 * w + 8] = dark;
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 16f);
    }
}
