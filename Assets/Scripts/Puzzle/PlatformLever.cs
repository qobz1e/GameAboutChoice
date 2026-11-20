using UnityEngine;

public sealed class PlatformLever : MonoBehaviour
{
    [SerializeField] LeverPlatform platform;
    [SerializeField] Transform handle;
    [SerializeField] SpriteRenderer indicator;
    [SerializeField, Min(.1f)] float reach = 1.6f;
    Runner player;

    void Start() => player = FindAnyObjectByType<Runner>();
    public bool TryInteract(Runner actor)
    {
        if(!actor || !actor.isActiveAndEnabled ||
            Vector2.Distance(actor.transform.position,transform.position) > reach) return false;
        return platform.Activate();
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.E)) TryInteract(player);
        if(handle) handle.localRotation = Quaternion.Euler(0,0,platform.HeadingToEnd ? -35 : 35);
        if(indicator) indicator.color = platform.IsMoving
            ? new Color(1,.67f,.24f) : new Color(.60f,.77f,.85f);
    }
}
