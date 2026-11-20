using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point: -executeMethod RoutePlayCheck.Verify (omit -quit).
public static class RoutePlayCheck
{
    const string Pending = "DiplomaGame.RoutePlayCheck";
    static IEnumerator scenario;
    static double deadline;
    static bool entered;

    public static void Verify()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run on a batch test copy.");
        EditorSceneManager.OpenScene("Assets/Scenes/RouteChoice.unity");
        SessionState.SetBool(Pending, true);
        Register();
        EditorApplication.EnterPlaymode();
    }

    [InitializeOnLoadMethod]
    static void Register()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Combat Play Mode timeout");
            if (!EditorApplication.isPlaying || Time.frameCount < 2) return;
            if (!entered)
            {
                entered = true;
                scenario = Encounter();
            }
            if (scenario.MoveNext()) return;
            Finish(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        SessionState.EraseBool(Pending);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }


    static IEnumerator Encounter()
    {
        for (int route = 0; route < 2; route++)
        {
            var module = UnityEngine.Object.FindAnyObjectByType<RouteChoiceModule>();
            int selections = 0, completions = 0, rewards = 0, coins = 0, exits = 0;
            module.routeSelected.AddListener(index => { Require(index == route, "Selection identity"); selections++; });
            module.challengeCompleted.AddListener(index => { Require(index == route, "Completion identity"); completions++; });
            module.coinRewardRequested.AddListener(amount => { rewards++; coins += amount; });
            module.exited.AddListener(() => exits++);
            Require(module.SelectedRoute == -1 && module.CombatCoins == 10 && module.PeacefulCoins == 10, "Initial offer");
            Require(!module.ChallengeCompleted && module.RequestedCoins == 0, "Early reward");
            module.SelectRoute(route);
            module.SelectRoute(1 - route);
            yield return null;
            Require(module.SelectedRoute == route && selections == 1 && completions == 0 && rewards == 0, "Selection must be distinct from completion");
            var actor = UnityEngine.Object.FindAnyObjectByType<Runner>();
            var health = actor.GetComponent<CombatHealth>();
            Require(health.Current == health.Maximum, "Route player");
            ChallengeCompletion source;
            if (route == 0)
            {
                var challenge = UnityEngine.Object.FindAnyObjectByType<CombatChallenge>();
                source = challenge.GetComponent<ChallengeCompletion>();
                foreach (var enemy in challenge.RequiredEnemies) enemy.TakeHit(enemy.Maximum);
                Require(!module.ChallengeCompleted && rewards == 0, "Reward before reaching door");
                actor.GetComponent<Rigidbody2D>().position = new Vector2(challenge.ExitDoor.transform.position.x, 1);
                Physics2D.SyncTransforms();
                while (!module.ChallengeCompleted) yield return null;
            }
            else
            {
                var challenge = UnityEngine.Object.FindAnyObjectByType<PeacefulChallenge>();
                source = challenge.GetComponent<ChallengeCompletion>();
                var block = UnityEngine.Object.FindAnyObjectByType<MovableBlock>().GetComponent<Rigidbody2D>();
                block.position = new Vector2(41.5f,9);
                actor.GetComponent<Rigidbody2D>().position = new Vector2(54,9);
                Physics2D.SyncTransforms();
                while (!module.ChallengeCompleted) yield return null;
            }
            Require(module.ChallengeCompleted && completions == 1 && rewards == 1 && coins == 10, "One reward request per completion");
            source.Complete();
            source.completed.Invoke();
            Require(completions == 1 && rewards == 1 && actor.GetComponent<CoinPurse>().Balance == 10, "Duplicate reward or incorrect coin balance");
            Require(actor.isActiveAndEnabled && Mathf.Abs(actor.GetComponent<Rigidbody2D>().position.x) < .1f, $"Shared exit arrival route {route}, active {actor.isActiveAndEnabled}, position {actor.GetComponent<Rigidbody2D>().position}");
            actor.GetComponent<Rigidbody2D>().position = new Vector2(5,1);
            Physics2D.SyncTransforms();
            while (!module.Exited) yield return null;
            Require(exits == 1, "Shared exit event");
            module.Restart();
            while (module) yield return null;
        }
        var last = UnityEngine.Object.FindAnyObjectByType<RouteChoiceModule>();
        int failedRewards = 0;
        last.coinRewardRequested.AddListener(_ => failedRewards++);
        last.SelectRoute(0);
        yield return null;
        var player = UnityEngine.Object.FindAnyObjectByType<Runner>().GetComponent<CombatHealth>();
        player.TakeHit(player.Maximum);
        Require(!last.ChallengeCompleted && failedRewards == 0, "Death awarded coins");
        last.Restart();
        while (last) yield return null;
        Require(UnityEngine.Object.FindAnyObjectByType<RouteChoiceModule>().SelectedRoute == -1, "Restart did not restore selection");
        Debug.Log("Route checks passed: offers, distinct selection/completion, both challenges, one reward request, shared exit and restart after death");
    }
}
