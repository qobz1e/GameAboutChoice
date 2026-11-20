using UnityEngine;

[DefaultExecutionOrder(10)]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class MovableBlock : MonoBehaviour
{
    [SerializeField] Runner player;
    [SerializeField] float leftOffset = -1, rightOffset = 4;
    [SerializeField] float speed = 6;
    Rigidbody2D body;
    BoxCollider2D box;
    Vector2 origin;
    public float Left => origin.x + leftOffset;
    public float Right => origin.x + rightOffset;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        origin = body.position;
    }

    void FixedUpdate()
    {
        if (!player || !player.isActiveAndEnabled) return;
        var actor = player.GetComponent<Rigidbody2D>();
        var bounds = player.GetComponent<BoxCollider2D>().bounds;
        var block = box.bounds;
        float direction = Mathf.Sign(block.center.x - bounds.center.x);
        float gap = Mathf.Abs(block.center.x - bounds.center.x) - block.extents.x - bounds.extents.x;
        if (gap > .06f || bounds.min.y >= block.max.y - .08f || bounds.max.y <= block.min.y + .08f) return;
        if (actor.linearVelocity.x * direction <= .01f) return;
        float x = Mathf.Clamp(body.position.x + direction * Mathf.Min(speed, Mathf.Abs(actor.linearVelocity.x)) * Time.fixedDeltaTime, Left, Right);
        body.MovePosition(new Vector2(x, origin.y));
    }
}
