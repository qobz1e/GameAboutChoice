using UnityEngine;

// Reusable endpoint travel; riders remain independent rigidbodies and may jump off.
[DefaultExecutionOrder(-30)]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class LeverPlatform : MonoBehaviour
{
    [SerializeField] Vector2 travel = new Vector2(8,0);
    [SerializeField, Min(.1f)] float speed = 2;
    [SerializeField, Min(0)] float boardingDelay = .65f;
    Rigidbody2D body;
    BoxCollider2D deck;
    Vector2 start, goal;
    float delay;
    bool active;
    public bool HeadingToEnd { get; private set; }
    public bool IsMoving => active;
    public Vector2 Start => start;
    public Vector2 End => start + travel;
    readonly Collider2D[] riders = new Collider2D[16];
    readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        deck = GetComponent<BoxCollider2D>();
        start = body.position;
        goal = start;
    }
    public bool Activate()
    {
        if(active) return false;
        HeadingToEnd = !HeadingToEnd;
        goal = HeadingToEnd ? End : Start;
        delay = boardingDelay;
        active = true;
        return true;
    }
    void FixedUpdate()
    {
        if(!active) return;
        if(delay > 0) { delay -= Time.fixedDeltaTime; return; }
        Vector2 next = Vector2.MoveTowards(body.position,goal,speed*Time.fixedDeltaTime);
        Vector2 delta = next-body.position;
        Bounds bounds = deck.bounds;
        int count = Physics2D.OverlapBox(new Vector2(bounds.center.x,bounds.max.y+.06f),
            new Vector2(bounds.size.x-.04f,.16f),0,filter,riders);
        for(int i=0;i<count;i++)
        {
            var actor = riders[i].GetComponent<Runner>();
            var rigidbody = riders[i].attachedRigidbody;
            if(!actor || !actor.isActiveAndEnabled || !rigidbody) continue;
            if(Mathf.Abs(riders[i].bounds.min.y-bounds.max.y) > .12f || rigidbody.linearVelocity.y > .1f) continue;
            rigidbody.position += delta;
        }
        body.MovePosition(next);
        if(Vector2.SqrMagnitude(next-goal) < .000001f) active=false;
    }
}
