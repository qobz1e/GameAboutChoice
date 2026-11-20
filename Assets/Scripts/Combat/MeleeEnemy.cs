using UnityEngine;

[RequireComponent(typeof(EnemySpriteAnimation))]
public sealed class MeleeEnemy : MonoBehaviour
{
    public enum Phase { Idle, Approach, Prepare, Strike, Recover, Defeated }
    public Phase State { get; private set; }
    [SerializeField] CombatHealth target;
    [SerializeField] Transform eye;
    [SerializeField] float detection = 6, speed = 2.1f, attackDistance = 1.35f;
    [SerializeField] float preparation = .55f, activeTime = .14f, recovery = .75f;

    static readonly Color warning = new Color(1, .62f, .12f, .34f);
    static readonly Color attack = new Color(1, .3f, .13f, .85f);
    MeleeStrike strike;
    CombatHealth health;
    float timer;
    int facing = -1;

    void Awake()
    {
        strike = GetComponent<MeleeStrike>();
        health = GetComponent<CombatHealth>();
        health.Defeated += OnDefeated;
    }

    void OnDestroy() { if (health) health.Defeated -= OnDefeated; }

    void Update()
    {
        float dt = Time.deltaTime;
        if (State == Phase.Defeated)
        {
            timer -= dt;
            transform.localScale = Vector3.one * Mathf.Clamp01(timer / .3f);
            if (timer <= 0) gameObject.SetActive(false);
            return;
        }
        if (!target.Alive) { strike.End(); return; }
        Vector2 offset = target.transform.position - transform.position;
        switch (State)
        {
            case Phase.Idle:
            case Phase.Approach:
                if (offset.sqrMagnitude > detection * detection) { State = Phase.Idle; return; }
                State = Phase.Approach;
                if (Mathf.Abs(offset.x) > .01f) facing = offset.x > 0 ? 1 : -1;
                GetComponent<SpriteRenderer>().flipX = facing < 0;
                eye.localPosition = new Vector3(.24f * facing, .18f, 0);
                if (Mathf.Abs(offset.x) <= attackDistance && Mathf.Abs(offset.y) < .85f)
                {
                    strike.Begin(facing, warning);
                    State = Phase.Prepare;
                    timer = preparation;
                }
                else
                {
                    var p = transform.position;
                    p.x = Mathf.MoveTowards(p.x, target.transform.position.x, speed * dt);
                    transform.position = p;
                }
                break;
            case Phase.Prepare:
                timer -= dt;
                if (timer > 0) break;
                State = Phase.Strike;
                timer = activeTime;
                strike.Sample(attack);
                break;
            case Phase.Strike:
                strike.Sample(attack);
                timer -= dt;
                if (timer > 0) break;
                strike.End();
                State = Phase.Recover;
                timer = recovery;
                break;
            case Phase.Recover:
                timer -= dt;
                if (timer <= 0) State = Phase.Idle;
                break;
        }
    }

    void OnDefeated()
    {
        strike.End();
        GetComponent<BoxCollider2D>().enabled = false;
        State = Phase.Defeated;
        timer = .3f;
    }
}

