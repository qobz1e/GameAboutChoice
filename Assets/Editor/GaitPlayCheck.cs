using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point: -executeMethod GaitPlayCheck.Verify (omit -quit).
public static class GaitPlayCheck
{
    const string Pending = "DiplomaGame.GaitPlayCheck";
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Gait Play Mode timeout");
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
        var player = UnityEngine.Object.FindAnyObjectByType<Runner>();
        var body = player.GetComponent<Rigidbody2D>();
        var profile = player.GetComponent<SpriteRenderer>();
        player.gameObject.AddComponent<PuzzleTestInput>();
        body.simulated = false;
        player.transform.position = new Vector3(10.3f,.56f,0);
        body.position = new Vector2(10.3f,.56f);
        body.linearVelocity = Vector2.zero;
        body.simulated = true;
        float until = Time.time + .15f;
        while (Time.time < until) yield return null;
        Require(profile.sprite.name == "HoodIdle", "Initial idle");
        foreach (int direction in new[] { 1, -1 })
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            PuzzleTestInput.Move = direction;
            typeof(Runner).GetField("facing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(player, direction);
            until = Time.time + .72f;
            string previous = "";
            int lastFrame = 0;
            while (Time.time < until)
            {
                string name = profile.sprite.name;
                if (name.StartsWith("HoodWalk") && name != previous)
                {
                    int frame = int.Parse(name.Substring("HoodWalk".Length));
                    if (lastFrame != 0) Require(frame == lastFrame % 6 + 1, "Walk sequence order");
                    lastFrame = frame;
                    seen.Add(name);
                    previous = name;
                }
                yield return null;
            }
            Require(seen.Count == 6, "Missing walk frames");
            Require(profile.flipX == (direction < 0), "Animation changed facing");
            PuzzleTestInput.Move = 0;
            until = Time.time + .15f;
            while (Time.time < until) yield return null;
            Require(profile.sprite.name == "HoodIdle", "Stopped character did not return to idle");
        }
        Debug.Log("Six-frame gait: order, both directions, and return to IDLE passed");
    }
}

