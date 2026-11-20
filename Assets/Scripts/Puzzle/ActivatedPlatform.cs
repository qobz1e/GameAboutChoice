using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class ActivatedPlatform : MonoBehaviour
{
    [SerializeField] PressurePlate plate;
    public bool Active => GetComponent<BoxCollider2D>().enabled;

    void OnEnable()
    {
        plate.changed.AddListener(SetActive);
        SetActive(plate.Active);
    }

    void OnDisable() => plate.changed.RemoveListener(SetActive);

    void SetActive(bool active)
    {
        GetComponent<BoxCollider2D>().enabled = active;
        GetComponent<SpriteRenderer>().color = active
            ? new Color(.85f, .85f, .85f, 1) : new Color(.85f, .85f, .85f, .48f);
    }
}
