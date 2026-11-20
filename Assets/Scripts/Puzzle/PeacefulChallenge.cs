using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public sealed class PeacefulChallenge : MonoBehaviour
{
    [SerializeField] PressurePlate plate;
    [SerializeField] CombatHealth player;
    [SerializeField] BoxCollider2D exit;
    public bool Completed { get; private set; }
    public UnityEvent completed = new UnityEvent();

    void FixedUpdate()
    {
        if (Completed || !plate.Active || !player.Alive) return;
        if (!exit.bounds.Intersects(player.GetComponent<BoxCollider2D>().bounds)) return;
        Completed = true;
        exit.GetComponent<SpriteRenderer>().color = new Color(.35f, 1, .6f);
        completed.Invoke();
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
