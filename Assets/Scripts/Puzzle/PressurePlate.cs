using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class PressurePlate : MonoBehaviour
{
    public bool Active { get; private set; }
    public UnityEvent<bool> changed = new UnityEvent<bool>();
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
    BoxCollider2D sensor;
    SpriteRenderer visual;

    void Awake()
    {
        sensor = GetComponent<BoxCollider2D>();
        visual = GetComponent<SpriteRenderer>();
        visual.color = new Color(.72f, .76f, .8f);
    }

    void FixedUpdate()
    {
        sensor.Overlap(filter, overlaps);
        bool pressed = false;
        foreach (var other in overlaps)
            if (other.TryGetComponent<MovableBlock>(out _) || other.GetComponentInParent<Runner>())
                pressed = true;
        if (pressed == Active) return;
        Active = pressed;
        visual.color = Active ? new Color(1, .92f, .67f) : new Color(.72f, .76f, .8f);
        changed.Invoke(Active);
    }
}

