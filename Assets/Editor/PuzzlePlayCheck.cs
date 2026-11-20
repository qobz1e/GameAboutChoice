using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point: -executeMethod PuzzlePlayCheck.Verify (omit -quit).
public static class PuzzlePlayCheck
{
    const string Pending = "DiplomaGame.PuzzlePlayCheck";
    static IEnumerator scenario;
    static double deadline;
    static bool entered;

    public static void Verify()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run on a batch test copy.");
        EditorSceneManager.OpenScene("Assets/Scenes/PeacefulTest.unity");
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Puzzle Play Mode timeout");
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


    static System.Reflection.FieldInfo Field(string name) =>
        typeof(Runner).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

    static IEnumerator Step()
    {
        float time = Time.fixedTime;
        while (Time.fixedTime == time) yield return null;
    }

    static IEnumerator JumpTo(Runner player, float target, bool dash)
    {
        var body = player.GetComponent<Rigidbody2D>();
        Field("jump").SetValue(player, true);
        float start = Time.time;
        bool dashed = false;
        bool airborne = false;
        while (Time.time - start < 2)
        {
            float delta = target - body.position.x;
            PuzzleTestInput.Move = Mathf.Abs(delta) > .12f ? Mathf.Sign(delta) : 0f;
            if (dash && !dashed && Time.time - start > .18f)
            {
                player.SendMessage("RequestDash", 1);
                dashed = true;
            }
            if (Mathf.Abs(body.linearVelocity.y) > 1) airborne = true;
            if (airborne && Mathf.Abs(delta) < .3f && Mathf.Abs(body.linearVelocity.y) < .05f)
                yield break;
            if (body.position.y < 6) throw new Exception("Puzzle jump failed: " + body.position);
            yield return null;
        }
        throw new Exception("Jump did not land: " + body.position);
    }

    static IEnumerator Encounter()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<Runner>();
        // Supply deterministic controls while retaining the normal physics controller.
        player.gameObject.AddComponent<PuzzleTestInput>();
        var body = player.GetComponent<Rigidbody2D>();
        var block = UnityEngine.Object.FindAnyObjectByType<MovableBlock>();
        var plate = UnityEngine.Object.FindAnyObjectByType<PressurePlate>();
        var bridge = UnityEngine.Object.FindAnyObjectByType<ActivatedPlatform>();
        var challenge = UnityEngine.Object.FindAnyObjectByType<PeacefulChallenge>();
        int completed = 0;
        challenge.completed.AddListener(() => completed++);
        Require(!plate.Active && !bridge.Active && !challenge.Completed, "Puzzle initial state");

        // Player weight activates the plate; stepping away releases it without completing the challenge.
        body.position = new Vector2(41.5f, 9);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        float settle = Time.time + .3f;
        while (Time.time < settle) yield return null;
        Require(plate.Active && bridge.Active && !challenge.Completed, "Player weight did not activate the plate and bridge");
        body.position = new Vector2(68, 9);
        Physics2D.SyncTransforms();
        yield return null;
        yield return null;
        settle = Time.time + .15f;
        while (Time.time < settle) yield return null;
        Require(!plate.Active && !bridge.Active, "Plate did not release after player left");
        Require(!challenge.Completed, "Exit completed without the plate");

        player.enabled = true;
        body.position = new Vector2(block.transform.position.x - 1.1f, 8.96f);
        Physics2D.SyncTransforms();
        player.SendMessage("RequestDash", 1);
        float until = Time.time + 2;
        while (!plate.Active && Time.time < until)
        {
            Require(Mathf.Abs(block.GetComponent<Rigidbody2D>().linearVelocity.x) <= 6.01f, "Dash exceeded block speed cap");
            PuzzleTestInput.Move = 1;
            yield return null;
        }
        PuzzleTestInput.Move = 0;
        Require(plate.Active && bridge.Active, "Block did not activate bridge");
        body.position = new Vector2(block.transform.position.x + 1.1f, 8.96f);
        Physics2D.SyncTransforms();
        // Push back from the opposite side.
        until = Time.time + .6f;
        while (Time.time < until)
        {
            PuzzleTestInput.Move = -1;
            yield return null;
        }
        Require(!plate.Active && !bridge.Active, "Bridge stayed active after removal");
        body.position = new Vector2(block.transform.position.x - 1.1f, 8.96f);
        Physics2D.SyncTransforms();
        until = Time.time + 2;
        while (!plate.Active && Time.time < until)
        {
            Require(Mathf.Abs(block.GetComponent<Rigidbody2D>().linearVelocity.x) <= 6.01f, "Dash exceeded block speed cap");
            PuzzleTestInput.Move = 1;
            yield return null;
        }
        PuzzleTestInput.Move = 0;
        Require(plate.Active, $"Could not replace block: player {body.position}, block {block.transform.position}");
        // Cross the block, then cross to the bridge and exit using normal jumps.
        var crossing = JumpTo(player, 42.7f, false);
        while (crossing.MoveNext()) yield return crossing.Current;
        crossing = JumpTo(player, 46.7f, true);
        while (crossing.MoveNext()) yield return crossing.Current;
        crossing = JumpTo(player, 51.6f, false);
        while (crossing.MoveNext()) yield return crossing.Current;
        // Walk to the new lever, board during its departure delay, then ride without input.
        var ferry = UnityEngine.Object.FindAnyObjectByType<LeverPlatform>();
        var lever = GameObject.Find("Departure lever").GetComponent<PlatformLever>();
        until = Time.time + 2;
        while(body.position.x < 55.8f && Time.time < until) { PuzzleTestInput.Move=1; yield return null; }
        PuzzleTestInput.Move=0;
        Require(!challenge.Completed, "Challenge completed before ferry crossing");
        Require(lever.TryInteract(player), "Nearby lever did not start ferry");
        Require(!lever.TryInteract(player), "Repeated interaction reversed moving ferry");
        Field("jump").SetValue(player,true);
        float boardedAt=Time.time;
        bool boarded=false;
        until=Time.time+2;
        while(Time.time<until)
        {
            float dx=ferry.transform.position.x-body.position.x;
            PuzzleTestInput.Move=Mathf.Abs(dx)>.12f ? Mathf.Sign(dx):0;
            if(Time.time-boardedAt>.5f && Mathf.Abs(dx)<.6f && Mathf.Abs(body.linearVelocity.y)<.05f)
            {boarded=true;break;}
            yield return null;
        }
        PuzzleTestInput.Move=0;
        Require(boarded,"Could not jump onto departing ferry");
        float rideOffset=body.position.x-ferry.transform.position.x;
        until=Time.time+5;
        while(ferry.IsMoving && Time.time<until)
        {
            Require(Mathf.Abs(body.position.x-ferry.transform.position.x-rideOffset)<.2f,"Rider slipped off moving platform");
            yield return null;
        }
        until=Time.time+.1f;
        while(Time.time<until)yield return null;
        Require(!ferry.IsMoving && Vector2.Distance(ferry.GetComponent<Rigidbody2D>().position,ferry.End)<.03f,"Ferry did not stop at endpoint");
        crossing=JumpTo(player,66.3f,false);
        while(crossing.MoveNext())yield return crossing.Current;
        var returnLever=GameObject.Find("Arrival lever").GetComponent<PlatformLever>();
        Require(returnLever.TryInteract(player),"Return lever did not recall ferry");

        until = Time.time + 2;
        while (!challenge.Completed && Time.time < until)
        {
            Require(Mathf.Abs(block.GetComponent<Rigidbody2D>().linearVelocity.x) <= 6.01f, "Dash exceeded block speed cap");
            PuzzleTestInput.Move = 1;
            yield return null;
        }
        PuzzleTestInput.Move = 0;
        Require(challenge.Completed && completed == 1, "Challenge did not complete exactly once");
        yield return null;
        Require(completed == 1, "Duplicate completion");
        challenge.Restart();
        while (challenge) yield return null;
        challenge = UnityEngine.Object.FindAnyObjectByType<PeacefulChallenge>();
        block = UnityEngine.Object.FindAnyObjectByType<MovableBlock>();
        plate = UnityEngine.Object.FindAnyObjectByType<PressurePlate>();
        bridge = UnityEngine.Object.FindAnyObjectByType<ActivatedPlatform>();
        Require(!UnityEngine.Object.FindAnyObjectByType<LeverPlatform>().IsMoving &&
            !challenge.Completed && !plate.Active && !bridge.Active &&
            Mathf.Abs(block.transform.position.x - 38.5f) < .01f, "Restart failed to reset puzzle");
        Debug.Log("Puzzle checks passed: automatic pushing from both sides and dash speed cap, plate, bridge, ferry boarding, passenger carry, endpoints, return lever, completion and restart");
    }
}

[DefaultExecutionOrder(100)]
public sealed class PuzzleTestInput : MonoBehaviour
{
    public static float Move;
    Runner player;
    void Awake() => player = GetComponent<Runner>();
    void Update()
    {
        typeof(Runner).GetField("move", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .SetValue(player, Move);
    }
}



