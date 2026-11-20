using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point: -executeMethod DashPlayCheck.Verify (omit -quit).
public static class DashPlayCheck
{
    const string Pending = "DiplomaGame.DashPlayCheck";
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
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Dash Play Mode timeout");
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
        var runner = UnityEngine.Object.FindAnyObjectByType<Runner>();
        var body = runner.GetComponent<Rigidbody2D>();
        var wind = runner.GetComponent<DashWind>();
        var profile = runner.GetComponent<SpriteRenderer>();
        Require(wind != null, "Shared player missing dash wind");
        var idle = Resources.Load<Sprite>("Textures/Characters/HoodIdle");
        var jump = Resources.Load<Sprite>("Textures/Characters/HoodJump");
        Require(jump.texture == idle.texture && jump.pixelsPerUnit == idle.pixelsPerUnit,
            "Jump and idle must use the same undistorted texture and scale");
        float until = Time.time + .1f;
        while(Time.time < until) yield return null;
        Require(wind.VisibleCount == 0, "Wind emitted while idle");
        foreach(int direction in new[]{1,-1})
        {
            body.simulated = false;
            runner.transform.position = new Vector3(12,3,0);
            body.position = new Vector2(12,3);
            body.linearVelocity = new Vector2(0,-1);
            body.simulated = true;
            typeof(Runner).GetField("dashReady",System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(runner,true);
            runner.SendMessage("RequestDash",direction);
            until = Time.time + .07f;
            while(Time.time < until) yield return null;
            Require(runner.IsDashing && wind.VisibleCount > 0, "Dash did not emit wind");
            Require(body.linearVelocity.x * direction > 10 && body.linearVelocity.y < -1, "Dash physics changed");
            Require(profile.sprite == jump, "Air dash lost jump pose");
            foreach(var sr in runner.GetComponentsInChildren<SpriteRenderer>())
            {
                if(sr.gameObject.name != "Dash wind streak" || !sr.enabled) continue;
                Require((sr.transform.position.x-runner.transform.position.x)*direction < 0, "Wind appeared ahead of player");
                Require(sr.color.a > 0 && sr.color.a < .6f, "Wind should be translucent");
            }
            until = Time.time + .5f;
            while(Time.time < until) yield return null;
            Require(!runner.IsDashing && wind.VisibleCount == 0,"Wind did not fade after dash");
        }
        runner.enabled = false;
        yield return null;
        Require(wind.VisibleCount == 0, "Disabled player retained wind");
        Debug.Log("Dash visuals passed: both directions, translucent trailing streaks, fade, gravity, idle and jump proportions.");
    }
}

