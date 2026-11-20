using UnityEngine;

[DefaultExecutionOrder(20)]
public sealed class PlayerMelee : MonoBehaviour
{
    [SerializeField] float activeTime = .14f;
    [SerializeField] float recoveryTime = .23f;
    static readonly Color swingColor = new Color(.35f, .96f, 1, .8f);
    SpriteRenderer profile;
    MeleeStrike strike;
    CombatHealth health;
    float activeLeft, recoveryLeft;

    void Awake()
    {
        profile = GetComponent<SpriteRenderer>();
        strike = GetComponent<MeleeStrike>();
        health = GetComponent<CombatHealth>();
    }

    public bool Ready => isActiveAndEnabled && health.Alive && activeLeft <= 0 && recoveryLeft <= 0;

    public void TryAttack()
    {
        if (!Ready) return;
        strike.Begin(profile.flipX ? -1 : 1, swingColor);
        activeLeft = activeTime;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J)) TryAttack();
        if (activeLeft > 0)
        {
            strike.Sample(swingColor);
            activeLeft -= Time.deltaTime;
            if (activeLeft <= 0)
            {
                strike.End();
                recoveryLeft = recoveryTime;
            }
        }
        else recoveryLeft = Mathf.Max(0, recoveryLeft - Time.deltaTime);
    }

    void OnDisable() { if (strike) strike.End(); }
}
