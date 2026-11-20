using UnityEngine;

[ExecuteAlways]
public sealed class CaveTorch : MonoBehaviour
{
    public Transform flame;
    public SpriteRenderer glow;
    Vector3 flameScale;
    void OnEnable()
    {
        if (flame) flameScale = flame.localScale;
    }
    void Update()
    {
        if (!flame || !glow) return;
        float time = Mathf.Floor(Time.realtimeSinceStartup * 9) / 9;
        float wave = Mathf.Sin(time * 7 + transform.position.x * 1.7f);
        flame.localScale = new Vector3(flameScale.x * (1 + wave * .05f), flameScale.y * (1 + wave * .08f), 1);
        glow.color = new Color(1, .42f, .10f, .27f + wave * .025f);
    }
    void OnDisable()
    {
        if (flame) flame.localScale = flameScale;
    }
}
