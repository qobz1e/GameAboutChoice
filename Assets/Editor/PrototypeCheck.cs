using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PrototypeCheck
{
    public static void Verify()
    {

        foreach (string scene in new[] { "MainMenu", "PeacefulTest", "CombatTest" })
            if (!System.Array.Exists(EditorBuildSettings.scenes, entry => entry.enabled && entry.path == "Assets/Scenes/" + scene + ".unity"))
                throw new Exception("Missing test scene: " + scene);
        Physics2D.simulationMode = SimulationMode2D.Script;
        try
        {
            CheckSingleWall();
            CheckWallRoute();
            CheckAirDash(8f, false);
            CheckAirDash(-4f, false);
            CheckAirDash(-4f, true);
            CheckDashInputs("PeacefulTest");
            CheckDashInputs("CombatTest");
            CheckGap(6.9f, 9.5f, false, true);
            CheckGap(16.3f, 21.4f, false, false);
            CheckGap(15.25f, 21.4f, true, true, 10);
            CheckGap(15.25f, 21.4f, true, true, 24);
        }
        finally
        {
            Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }
    }

    static void CheckSingleWall()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PeacefulTest.unity");
        var runner = UnityEngine.Object.FindAnyObjectByType<Runner>();
        runner.SendMessage("Awake");
        var body = runner.GetComponent<Rigidbody2D>();
        var feet = runner.GetComponent<BoxCollider2D>();
        var contacts = new ContactPoint2D[8];
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var move = typeof(Runner).GetField("move", flags);
        var jump = typeof(Runner).GetField("jump", flags);
        body.position = new Vector2(31, 1);
        Physics2D.SyncTransforms();
        bool launched = false;
        bool rejected = false;
        for (int tick = 0; tick < 150; tick++)
        {
            bool wall = false, floor = false;
            for (int i = 0, n = feet.GetContacts(contacts); i < n; i++)
            {
                var normal = contacts[i].normal;
                wall |= normal.x < -.9f;
                floor |= normal.y > .65f;
            }
            move.SetValue(runner, 1f);
            jump.SetValue(runner, wall);
            float before = body.linearVelocity.y;
            runner.SendMessage("FixedUpdate");
            if (wall && !launched && body.linearVelocity.x < 0)
            {
                launched = true;
                // Attempt to dash straight back on the next tick.
                runner.SendMessage("RequestDash", 1);
            }
            else if (launched && !wall && before > 10 && body.linearVelocity.x >= 0)
                throw new Exception("Reversed during the wall jump lock");
            else if (launched && wall && !floor)
            {
                if (body.linearVelocity.y > before + .01f)
                    throw new Exception("Second impulse from the same wall");
                rejected = true;
            }
            else if (rejected && floor && wall && body.linearVelocity.x < 0 && body.linearVelocity.y > 0)
            {
                Debug.Log("Single-wall repeat blocked until landing");
                return;
            }
            Physics2D.Simulate(Time.fixedDeltaTime);
        }
        throw new Exception("Single-wall check did not cover a return and landing");
    }

    static void CheckWallRoute()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PeacefulTest.unity");
        var runner = UnityEngine.Object.FindAnyObjectByType<Runner>();
        runner.SendMessage("Awake");
        var body = runner.GetComponent<Rigidbody2D>();
        var feet = runner.GetComponent<BoxCollider2D>();
        var deck = GameObject.Find("Summit platform").GetComponent<BoxCollider2D>();
        var contacts = new ContactPoint2D[8];
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var move = typeof(Runner).GetField("move", flags);
        var jump = typeof(Runner).GetField("jump", flags);
        body.position = new Vector2(26, .6f);
        Physics2D.SyncTransforms();
        int heading = 1;
        int rebounds = 0;
        for (int tick = 0; tick < 500; tick++)
        {
            bool onFloor = false;
            int away = 0;
            for (int i = 0, count = feet.GetContacts(contacts); i < count; i++)
            {
                var normal = contacts[i].normal;
                if (normal.y > .65f) onFloor = true;
                if (Mathf.Abs(normal.x) > .9f) away = normal.x > 0 ? 1 : -1;
            }
            if (onFloor && deck.bounds.Contains(new Vector3(body.position.x, deck.bounds.center.y, 0))
                && feet.bounds.min.y >= deck.bounds.max.y - .04f && rebounds >= 2)
            {
                Debug.Log("Reached summit after wall jumps: " + rebounds);
                return;
            }
            bool push = away != 0 && away == -heading;
            bool hopFromTop = onFloor && body.position.y > 7 && body.position.x < 30;
            if (hopFromTop) heading = 1;
            move.SetValue(runner, (float)heading);
            jump.SetValue(runner, push || hopFromTop);
            runner.SendMessage("FixedUpdate");
            if (push)
            {
                if (Mathf.Sign(body.linearVelocity.x) != away || body.linearVelocity.y <= 0)
                    throw new Exception("Wall jump direction");
                heading = away;
                rebounds++;
            }
            Physics2D.Simulate(Time.fixedDeltaTime);
        }
        throw new Exception("Summit unreachable on test route: " + body.position + ", rebounds " + rebounds);
    }

    static void CheckAirDash(float initialVerticalSpeed, bool fastFall)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PeacefulTest.unity");
        var runner = UnityEngine.Object.FindAnyObjectByType<Runner>();
        var reference = UnityEngine.Object.Instantiate(runner);
        var dashing = runner.GetComponent<Rigidbody2D>();
        var falling = reference.GetComponent<Rigidbody2D>();
        runner.SendMessage("Awake");
        reference.SendMessage("Awake");
        dashing.position = new Vector2(0, 12);
        falling.position = new Vector2(8, 12);
        dashing.linearVelocity = falling.linearVelocity = Vector2.up * initialVerticalSpeed;
        var drop = typeof(Runner).GetField("drop", BindingFlags.Instance | BindingFlags.NonPublic);
        drop.SetValue(runner, fastFall);
        drop.SetValue(reference, fastFall);
        Physics2D.SyncTransforms();
        runner.SendMessage("RequestDash", 1);
        for (int tick = 0; tick < 15; tick++)
        {
            runner.SendMessage("FixedUpdate");
            reference.SendMessage("FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
            if (Mathf.Abs(dashing.position.y - falling.position.y) > .0001f ||
                Mathf.Abs(dashing.linearVelocity.y - falling.linearVelocity.y) > .0001f)
                throw new Exception("Dash changed the vertical trajectory");
        }
        if (dashing.position.x < 1) throw new Exception("Air dash did not move horizontally");
        Debug.Log("Verified air dash, initial vertical speed " + initialVerticalSpeed + ", fast fall " + fastFall);
    }

    static void CheckDashInputs(string scene)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
        var runner = UnityEngine.Object.FindAnyObjectByType<Runner>();
        runner.SendMessage("Awake");
        var profile = runner.GetComponent<SpriteRenderer>();
        if (!profile.sprite || profile.sprite.name != "HoodIdle")
            throw new Exception("Player sprite missing in " + scene);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var tap = typeof(Runner).GetMethod("RegisterTap", flags);
        var pending = typeof(Runner).GetField("dash", flags);
        void Tap(int key, float time, bool enabled = true) =>
            tap.Invoke(runner, new object[] { key, time, enabled });
        Tap(1, 0);
        Tap(1, .4f);
        Tap(-1, .45f);
        Tap(-1, .5f, false);
        if ((bool)pending.GetValue(runner)) throw new Exception("Unexpected dash");
        foreach (int direction in new[] { -1, 1 })
        {
            runner.SendMessage("Awake");
            Tap(direction, 1);
            Tap(direction, 1.2f);
            if (!(bool)pending.GetValue(runner)) throw new Exception("Double tap ignored");
            typeof(Runner).GetField("dashReady", flags).SetValue(runner, true);
            typeof(Runner).GetField("dashLeft", flags).SetValue(runner, 0f);
            runner.SendMessage("FixedUpdate");
            if (profile.flipX != (direction < 0) || Mathf.Sign(runner.GetComponent<Rigidbody2D>().linearVelocity.x) != direction)
                throw new Exception("Incorrect facing or dash direction");
        }
        Debug.Log("Verified sprite and directional double taps in " + scene);
    }

    static void CheckGap(float takeoff, float landing, bool dash, bool expectSuccess, int dashTick = 18)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/PeacefulTest.unity");
        var runner = UnityEngine.Object.FindAnyObjectByType<Runner>();
        var body = runner.GetComponent<Rigidbody2D>();
        runner.SendMessage("Awake");
        body.position = new Vector2(takeoff, .65f);
        Physics2D.SyncTransforms();
        void Tick()
        {
            runner.SendMessage("FixedUpdate");
            Physics2D.Simulate(Time.fixedDeltaTime);
        }
        for (int i = 0; i < 20; i++) Tick();
        void Set(string field, object value) => typeof(Runner).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(runner, value);
        Set("move", 1f);
        Set("jump", true);
        bool landed = false;
        for (int i = 0; i < 100; i++)
        {
            if (dash && i == dashTick) Set("dash", true);
            Tick();
            if (body.position.x >= landing && Mathf.Abs(body.position.y - .55f) < .05f && Mathf.Abs(body.linearVelocity.y) < .05f)
            {
                landed = true;
                break;
            }
            if (body.position.y < -3) break;
        }
        Debug.Log("Gap check: takeoff " + takeoff + ", dash " + dash + ", landed " + landed + ", position " + body.position);
        if (landed != expectSuccess) throw new Exception("Unexpected gap result");
    }
}

