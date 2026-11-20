using UnityEngine;

// A small pool of world-space streaks; presentation never drives player physics.
public sealed class DashWind : MonoBehaviour
{
    [SerializeField] Sprite pixel;
    const int Capacity = 30;
    const float Lifetime = .22f, Interval = .025f;
    readonly SpriteRenderer[] streaks = new SpriteRenderer[Capacity];
    readonly Vector3[] positions = new Vector3[Capacity];
    readonly float[] ages = new float[Capacity], lengths = new float[Capacity];
    readonly int[] directions = new int[Capacity];
    Runner runner;
    SpriteRenderer player;
    Vector3 previous;
    float emission;
    int next;
    bool wasDashing;
    public int VisibleCount { get; private set; }

    void Awake()
    {
        runner = GetComponent<Runner>();
        player = GetComponent<SpriteRenderer>();
        for (int i = 0; i < Capacity; i++)
        {
            var piece = new GameObject("Dash wind streak");
            piece.transform.SetParent(transform, false);
            var sr = piece.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.sortingLayerID = player.sortingLayerID;
            sr.sortingOrder = player.sortingOrder - 1;
            sr.enabled = false;
            streaks[i] = sr;
            ages[i] = Lifetime;
        }
        previous = transform.position;
    }

    void LateUpdate()
    {
        if (!runner.isActiveAndEnabled || Vector3.Distance(previous, transform.position) > 4)
            Clear();
        previous = transform.position;
        bool dashing = runner.isActiveAndEnabled && runner.IsDashing;
        if (dashing)
        {
            if (!wasDashing) emission = Interval;
            emission += Time.deltaTime;
            if (emission >= Interval)
            {
                emission %= Interval;
                int direction = player.flipX ? -1 : 1;
                for (int lane = 0; lane < 3; lane++)
                {
                    int i = next++ % Capacity;
                    lengths[i] = .38f + .12f * ((i + lane) % 3);
                    positions[i] = transform.position + new Vector3(
                        -direction * (.38f + lengths[i] * .5f), (lane - 1) * .24f + (i % 2) * .035f, 0);
                    directions[i] = direction;
                    ages[i] = 0;
                }
            }
        }
        else emission = 0;
        wasDashing = dashing;
        VisibleCount = 0;
        for (int i = 0; i < Capacity; i++)
        {
            ages[i] += Time.deltaTime;
            var sr = streaks[i];
            sr.enabled = ages[i] < Lifetime;
            if (!sr.enabled) continue;
            VisibleCount++;
            float remaining = 1 - ages[i] / Lifetime;
            positions[i].x -= directions[i] * Time.deltaTime * 1.4f;
            sr.transform.position = positions[i];
            // Compensate for the player's existing sprite scale, keeping the effect in world units.
            Vector3 scale = transform.lossyScale;
            sr.transform.localScale = new Vector3(
                lengths[i] * (.4f + .6f * remaining) / (pixel.bounds.size.x * Mathf.Abs(scale.x)),
                .026f / (pixel.bounds.size.y * Mathf.Abs(scale.y)), 1);
            sr.color = new Color(.94f, .97f, 1, .52f * remaining);
        }
    }

    void Clear()
    {
        for (int i = 0; i < Capacity; i++)
        {
            ages[i] = Lifetime;
            if (streaks[i]) streaks[i].enabled = false;
        }
        VisibleCount = 0;
        emission = 0;
        wasDashing = false;
    }
    void OnDisable() => Clear();
}
