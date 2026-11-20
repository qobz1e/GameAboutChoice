using UnityEngine;

[ExecuteAlways]
public sealed class CaveBackdrop : MonoBehaviour
{
    public Camera view;
    public SpriteRenderer artwork;
    void LateUpdate()
    {
        if (!view || !artwork) return;
        float height = view.orthographicSize * 2 + 2;
        float width = Mathf.Max(height * artwork.sprite.bounds.size.x / artwork.sprite.bounds.size.y,
                                view.orthographicSize * 2 * view.aspect + 3);
        artwork.transform.localScale = new Vector3(width / artwork.sprite.bounds.size.x,
                                                   height / artwork.sprite.bounds.size.y, 1);
        artwork.transform.localPosition = new Vector3(-Mathf.Sin(view.transform.position.x * .04f) * .4f, 0, 9);
    }
}
