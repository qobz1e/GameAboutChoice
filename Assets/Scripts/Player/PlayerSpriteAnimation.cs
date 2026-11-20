using UnityEngine;

[DefaultExecutionOrder(40)]
public sealed class PlayerSpriteAnimation : MonoBehaviour
{
    [SerializeField] Sprite idle, jump;
    [SerializeField] Sprite[] walkFrames;
    [SerializeField, Min(1)] float walkFramesPerSecond = 10;
    Rigidbody2D body;
    SpriteRenderer profile;
    BoxCollider2D feet;
    float walkTime;
    readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        profile = GetComponent<SpriteRenderer>();
        feet = GetComponent<BoxCollider2D>();
    }

    void LateUpdate()
    {
        bool grounded = false;
        for (int i = 0, count = feet.GetContacts(contacts); i < count; i++)
            grounded |= contacts[i].normal.y > .65f;
        if (!grounded && Mathf.Abs(body.linearVelocity.y) > .3f)
        {
            profile.sprite = jump;
            walkTime = 0;
            return;
        }
        if (Mathf.Abs(body.linearVelocity.x) <= .2f || walkFrames == null || walkFrames.Length == 0)
        {
            profile.sprite = idle;
            walkTime = 0;
            return;
        }
        profile.sprite = walkFrames[(int)walkTime % walkFrames.Length];
        walkTime = (walkTime + Time.deltaTime * walkFramesPerSecond) % walkFrames.Length;
    }
}
