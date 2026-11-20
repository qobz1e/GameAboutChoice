using UnityEngine;

// Presentation only: never changes enemy movement, facing, damage or phase timers.
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemySpriteAnimation : MonoBehaviour
{
    [SerializeField, Min(1)] float framesPerSecond = 10;
    Sprite idle;
    Sprite[] frames;
    SpriteRenderer profile;
    MeleeEnemy enemy;
    Vector3 lastPosition;
    float frameTime;

    void Awake()
    {
        profile = GetComponent<SpriteRenderer>();
        enemy = GetComponent<MeleeEnemy>();
        idle = profile.sprite;
        frames = new Sprite[6];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = Resources.Load<Sprite>("Textures/Characters/TrollWalk" + (i + 1));
    }

    void OnEnable() { lastPosition = transform.position; frameTime = 0; }

    void LateUpdate()
    {
        bool walking = enemy.State == MeleeEnemy.Phase.Approach &&
            Mathf.Abs(transform.position.x - lastPosition.x) > .00001f;
        lastPosition = transform.position;
        if (!walking)
        {
            profile.sprite = idle;
            frameTime = 0;
            return;
        }
        var frame = frames[(int)frameTime % frames.Length];
        profile.sprite = frame ? frame : idle;
        frameTime = (frameTime + Time.deltaTime * framesPerSecond) % frames.Length;
    }
}
