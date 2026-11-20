using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Runner : MonoBehaviour
{
    [SerializeField] float speed = 6f;
    [SerializeField] float jumpSpeed = 12f;
    [SerializeField] float gravity = 32f;
    [SerializeField] float dashSpeed = 17f;
    [SerializeField] float dashDuration = 0.18f;

    [SerializeField] float wallJumpSteerDelay = .16f;
    float wallJumpLock;
    int wallJumpDirection;
    int lastWallNormal;

    const float DoubleTapWindow = .24f;
    int lastTapDirection;
    float lastTapTime;
    int requestedDirection = 1;
    SpriteRenderer profile;
    Rigidbody2D body;
    BoxCollider2D feet;
    Vector2 spawn;
    float move;
    int facing = 1;
    bool jump, dash, drop, dashReady = true;
    float dashLeft;
    public bool IsDashing => dashLeft > 0;
    readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        feet = GetComponent<BoxCollider2D>();
        spawn = body.position;
        profile = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        move = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        if (move != 0 && wallJumpLock <= 0) facing = move > 0 ? 1 : -1;
        jump |= Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space);
        bool leftPressed = Input.GetKeyDown(KeyCode.A);
        bool rightPressed = Input.GetKeyDown(KeyCode.D);
        if (leftPressed && rightPressed) lastTapDirection = 0;
        else if (leftPressed || rightPressed)
            RegisterTap(leftPressed ? -1 : 1, Time.unscaledTime, PrototypeMenu.DoubleTapEnabled);

        if (Input.GetKeyDown(KeyCode.LeftShift) && !dash) RequestDash(facing);
        profile.flipX = (dashLeft > 0 ? dashDirection : facing) < 0;
        drop = Input.GetKey(KeyCode.S);
    }

    void RegisterTap(int direction, float now, bool enabled)
    {
        if (enabled && lastTapDirection == direction && now - lastTapTime <= DoubleTapWindow)
        {
            RequestDash(direction);
            lastTapDirection = 0;
            return;
        }
        lastTapDirection = enabled ? direction : 0;
        lastTapTime = now;
    }

    void RequestDash(int direction)
    {
        requestedDirection = direction;
        dash = true;
    }

    int dashDirection;
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        bool grounded = false;
        int wallNormal = 0;
        int count = feet.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            Vector2 normal = contacts[i].normal;
            if (normal.y > .65f) grounded = true;
            if (Mathf.Abs(normal.x) > .9f) wallNormal = normal.x > 0 ? 1 : -1;
        }
        if (grounded && body.linearVelocity.y <= 0)
            lastWallNormal = 0;
        bool wallJump = jump && wallJumpLock <= 0 && wallNormal != 0 && wallNormal != lastWallNormal;
        if (wallJump)
        {
            lastWallNormal = wallNormal;
            wallJumpDirection = wallNormal;
            wallJumpLock = wallJumpSteerDelay;
            facing = wallNormal;
            profile.flipX = facing < 0;
            dashLeft = 0;
            dash = false;
        }
        else if (grounded || (wallJumpLock <= 0 && move != 0))
        {
            wallJumpDirection = 0;
        }

        if (grounded && dashLeft <= 0) dashReady = true;
        if (dash && dashReady && dashLeft <= 0 && wallJumpLock <= 0)
        {
            wallJumpLock = 0;
            wallJumpDirection = 0;
            dashDirection = requestedDirection;
            facing = dashDirection;
            profile.flipX = facing < 0;
            dashLeft = dashDuration;
            dashReady = false;
        }

        float horizontal = (wallJumpDirection != 0 ? wallJumpDirection : move) * speed;
        wallJumpLock = Mathf.Max(0, wallJumpLock - dt);
        if (dashLeft > 0)
        {
            horizontal = dashDirection * dashSpeed;
            dashLeft -= dt;
        }

        float vertical = jump && (grounded || wallJump) ? jumpSpeed : body.linearVelocity.y;
        float fallGravity = drop && vertical < 0 ? gravity * 2 : gravity;
        body.linearVelocity = new Vector2(horizontal, Mathf.Max(-25, vertical - fallGravity * dt));

        jump = dash = false;
        if (body.position.y < -6)
        {
            body.position = spawn;
            body.linearVelocity = Vector2.zero;
            dashLeft = 0;
            dashReady = true;
            wallJumpDirection = 0;
            wallJumpLock = 0;
            facing = 1;
            lastTapDirection = 0;
            lastWallNormal = 0;
            profile.flipX = false;
        }
    }
}

