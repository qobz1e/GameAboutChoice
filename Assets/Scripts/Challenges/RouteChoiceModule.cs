using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public sealed class RouteChoiceModule : MonoBehaviour
{
    [SerializeField] GameObject[] routes;
    [SerializeField] ChallengeCompletion[] challenges;
    [SerializeField] Runner[] players;
    [SerializeField] SideCamera[] cameras;
    [SerializeField] RouteRewards rewards;
    [SerializeField] GameObject sharedExit;
    [SerializeField] Camera entranceCamera;
    [SerializeField] BoxCollider2D exitMarker;

    public int SelectedRoute { get; private set; } = -1;
    public bool ChallengeCompleted { get; private set; }
    public bool Exited { get; private set; }
    public int RequestedCoins { get; private set; }
    public int CombatCoins => rewards.combatCoins;
    public int PeacefulCoins => rewards.peacefulCoins;

    public UnityEvent<int> routeSelected = new UnityEvent<int>();
    public UnityEvent<int> challengeCompleted = new UnityEvent<int>();
    public UnityEvent<int> coinRewardRequested = new UnityEvent<int>();
    public UnityEvent exited = new UnityEvent();

    void Awake()
    {
        foreach (var challenge in challenges) challenge.completed.AddListener(OnCompleted);
    }

    void OnDestroy()
    {
        foreach (var challenge in challenges)
            if (challenge) challenge.completed.RemoveListener(OnCompleted);
    }

    public void SelectRoute(int route)
    {
        if (SelectedRoute >= 0 || route < 0 || route >= routes.Length) return;
        SelectedRoute = route;
        sharedExit.SetActive(false);
        entranceCamera.gameObject.SetActive(false);
        routes[route].SetActive(true);
        routeSelected.Invoke(route);
    }

    void OnCompleted()
    {
        if (SelectedRoute < 0 || ChallengeCompleted || !challenges[SelectedRoute].Completed) return;
        ChallengeCompleted = true;
        RequestedCoins = rewards.Coins(SelectedRoute);
        var actor = players[SelectedRoute];
        actor.transform.parent.SetParent(null, true);
        var camera = cameras[SelectedRoute];
        camera.transform.SetParent(null, true);
        camera.left = camera.right = 3;
        camera.maxHeight = 1.8f;
        routes[SelectedRoute].SetActive(false);
        sharedExit.SetActive(true);
        var body = actor.GetComponent<Rigidbody2D>();
        body.simulated = false;
        actor.transform.position = new Vector3(0, 1, 0);
        body.position = new Vector2(0, 1);
        body.linearVelocity = Vector2.zero;
        body.simulated = true;
        Physics2D.SyncTransforms();
        challengeCompleted.Invoke(SelectedRoute);
        actor.GetComponent<CoinPurse>().Credit(RequestedCoins);
        coinRewardRequested.Invoke(RequestedCoins);
    }

    void FixedUpdate()
    {
        if (!ChallengeCompleted || Exited) return;
        var actor = players[SelectedRoute];
        if (!actor.GetComponent<CombatHealth>().Alive ||
            !exitMarker.bounds.Intersects(actor.GetComponent<BoxCollider2D>().bounds)) return;
        Exited = true;
        exited.Invoke();
    }

    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
