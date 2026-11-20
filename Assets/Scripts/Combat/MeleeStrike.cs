using System.Collections.Generic;
using UnityEngine;

// The warning and the damage window share the same rectangle and locked facing.
public sealed class MeleeStrike : MonoBehaviour
{
    [SerializeField] SpriteRenderer area;
    CombatHealth owner;
    AttackVisual visual;
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    readonly ContactFilter2D hurtboxes = new ContactFilter2D { useTriggers = true };

    void Awake()
    {
        owner = GetComponent<CombatHealth>();
        visual = GetComponent<AttackVisual>();
    }
    [SerializeField] Vector2 size = new Vector2(1.65f, .82f);
    [SerializeField, Min(1)] int damage = 1;

    readonly HashSet<CombatHealth> hit = new HashSet<CombatHealth>();
    int facing;
    Bounds Zone => new Bounds(transform.position + Vector3.right * facing * (size.x / 2 - .12f),
                              new Vector3(size.x, size.y, 1));

    public void Begin(int direction, Color color)
    {
        facing = direction;
        hit.Clear();
        area.color = visual ? Color.clear : color;
        if (visual) visual.Begin(direction, owner.Team == CombatHealth.Faction.Enemy);
        var parentScale = area.transform.parent.lossyScale;
        var spriteSize = area.sprite.bounds.size;
        area.transform.localScale = new Vector3(size.x / (parentScale.x * spriteSize.x), size.y / (parentScale.y * spriteSize.y), 1);
        area.enabled = true;
        PositionArea();
    }

    void LateUpdate()
    {
        if (area.enabled) PositionArea();
    }

    void PositionArea()
    {
        area.transform.position = Zone.center;
    }

    public void Sample(Color color)
    {
        area.color = visual ? Color.clear : color;
        if (visual) visual.Strike();
        var zone = Zone;
        Physics2D.OverlapBox(zone.center, size, 0, hurtboxes, overlaps);
        foreach (var hurtbox in overlaps)
        {
            if (!hurtbox.TryGetComponent<CombatHealth>(out var target) ||
                target.Team == owner.Team || !target.Alive || hit.Contains(target)) continue;
            hit.Add(target);
            target.TakeHit(damage);
        }
    }

    public void End()
    {
        area.enabled = false;
        if (visual) visual.End();
    }
    void OnDisable() { if (area) End(); }
}
