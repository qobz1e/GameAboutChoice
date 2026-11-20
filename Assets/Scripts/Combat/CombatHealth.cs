using System;
using UnityEngine;

public sealed class CombatHealth : MonoBehaviour
{
    [SerializeField, Min(1)] int maximum = 3;
    public enum Faction { Player, Enemy }
    [SerializeField] Faction faction;
    public Faction Team => faction;
    public int Maximum => maximum;
    public int Current { get; private set; }
    public bool Alive => Current > 0;
    public event Action Defeated;

    SpriteRenderer body;
    Color usualColor;
    float flashTime;

    void Awake()
    {
        Current = maximum;
        body = GetComponent<SpriteRenderer>();
        usualColor = body.color;
    }

    public void TakeHit(int damage)
    {
        if (!Alive || damage <= 0) return;
        Current = Mathf.Max(0, Current - damage);
        flashTime = .14f;
        body.color = new Color(1, .26f, .24f);
        if (!Alive) Defeated?.Invoke();
    }

    void Update()
    {
        if (flashTime <= 0 || !Alive) return;
        flashTime -= Time.deltaTime;
        if (flashTime <= 0) body.color = usualColor;
    }
}
