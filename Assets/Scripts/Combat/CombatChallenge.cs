using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public sealed class CombatChallenge : MonoBehaviour
{
    public enum Result { InProgress, Completed, Failed }
    public Result Status { get; private set; }
    public CombatHealth Player => player;
    public CombatHealth[] RequiredEnemies => requiredEnemies;
    public int Remaining { get; private set; }
    public UnityEvent completed = new UnityEvent();

    [SerializeField] BoxCollider2D exitDoor;
    public BoxCollider2D ExitDoor => exitDoor;
    [SerializeField] CombatHealth player;
    [SerializeField] CombatHealth[] requiredEnemies;

    void Start()
    {
        Remaining = requiredEnemies.Length;
        player.Defeated += Fail;
        foreach (var enemy in requiredEnemies) enemy.Defeated += EnemyDefeated;
    }

    void OnDestroy()
    {
        if (player) player.Defeated -= Fail;
        foreach (var enemy in requiredEnemies)
            if (enemy) enemy.Defeated -= EnemyDefeated;
    }

    void EnemyDefeated()
    {
        if (Status != Result.InProgress) return;
        Remaining = Mathf.Max(0, Remaining - 1);
    }

    void FixedUpdate()
    {
        if (Status != Result.InProgress || Remaining != 0 || !player.Alive || !exitDoor) return;
        if (!exitDoor.bounds.Intersects(player.GetComponent<BoxCollider2D>().bounds)) return;
        Status = Result.Completed;
        completed.Invoke();
    }

    void Fail()
    {
        if (Status != Result.InProgress) return;
        Status = Result.Failed;
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
