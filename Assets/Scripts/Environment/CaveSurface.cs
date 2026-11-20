using UnityEngine;

// Copies visual state only. Gameplay keeps using its existing renderer and collider.
[ExecuteAlways]
public sealed class CaveSurface : MonoBehaviour
{
    public SpriteRenderer source;
    public SpriteRenderer stone;
    public SpriteRenderer rim;

    void LateUpdate()
    {
        if (!source || !stone) return;
        stone.color = source.color;
        if (rim) rim.color = new Color(.56f, .65f, .70f, source.color.a);
    }
}
