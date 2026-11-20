using UnityEngine;

public sealed class AttackVisual : MonoBehaviour
{
    [SerializeField] Sprite pixel;
    [SerializeField] bool club;
    Transform pivot;
    SpriteRenderer[] streaks;
    bool preparing, striking;
    float started;
    int facing;
    SpriteRenderer Piece(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(size.x / pixel.bounds.size.x, size.y / pixel.bounds.size.y, 1);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = pixel;
        sr.color = color;
        sr.sortingOrder = 7;
        return sr;
    }
    void Awake()
    {
        pivot = new GameObject("Weapon swing").transform;
        pivot.SetParent(transform, false);
        Piece(pivot,"Grip", new Vector2(.12f,0),new Vector2(.3f,.09f),new Color(.35f,.20f,.10f));
        if (club)
        {
            Piece(pivot,"Iron-bound club",new Vector2(.57f,0),new Vector2(.6f,.23f),new Color(.40f,.30f,.20f));
            Piece(pivot,"Iron band",new Vector2(.65f,0),new Vector2(.09f,.26f),new Color(.5f,.52f,.5f));
        }
        else
        {
            Piece(pivot,"Blade",new Vector2(.60f,0),new Vector2(.65f,.07f),new Color(.84f,.9f,.95f));
            Piece(pivot,"Guard",new Vector2(.26f,0),new Vector2(.07f,.26f),new Color(.70f,.55f,.28f));
        }
        streaks = new SpriteRenderer[6];
        for(int i=0;i<streaks.Length;i++)
            streaks[i] = Piece(pivot,"Slash trail",Vector2.zero,new Vector2(.2f,.035f),Color.clear);
        pivot.gameObject.SetActive(false);
    }
    public void Begin(int direction, bool windup)
    {
        facing = direction;
        preparing = windup;
        striking = !windup;
        started = Time.time;
        pivot.gameObject.SetActive(true);
    }
    public void Strike()
    {
        if (striking) return;
        preparing = false;
        striking = true;
        started = Time.time;
    }
    public void End()
    {
        preparing = striking = false;
        if (pivot) pivot.gameObject.SetActive(false);
    }
    void LateUpdate()
    {
        if (!preparing && !striking) return;
        float progress = Mathf.Clamp01((Time.time - started) / .14f);
        float angle = preparing ? 95 : Mathf.Lerp(75,-65,progress);
        pivot.localPosition = new Vector3(facing * .18f, -.05f, 0);
        pivot.localScale = new Vector3(facing,1,1);
        pivot.localRotation = Quaternion.Euler(0,0,facing*angle);
        for(int i=0;i<streaks.Length;i++)
        {
            float theta = (i*9 + 15) * Mathf.Deg2Rad;
            streaks[i].transform.localPosition = new Vector3(Mathf.Cos(theta)*.85f,Mathf.Sin(theta)*.85f,0);
            streaks[i].transform.localRotation = Quaternion.Euler(0,0,i*9+105);
            streaks[i].color = preparing ? Color.clear
                : new Color(1,club ? .65f : .92f,club ? .25f : 1, .75f*(1-progress)*(1-i/7f));
        }
    }
    void OnDisable() => End();
}
