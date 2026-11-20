using System;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point: -executeMethod CombatPlayCheck.Verify (omit -quit).
public static class CombatPlayCheck
{
    const string Pending = "DiplomaGame.CombatPlayCheck";
    static IEnumerator scenario;
    static double deadline;
    static bool entered;

    public static void Verify()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run on a batch test copy.");
        EditorSceneManager.OpenScene("Assets/Scenes/CombatTest.unity");
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
        var room = UnityEngine.Object.FindAnyObjectByType<CombatChallenge>();
        var enemy = UnityEngine.Object.FindAnyObjectByType<MeleeEnemy>();
        var enemyHP = room.RequiredEnemies[0];
        var player = room.Player;
        var body = player.GetComponent<Rigidbody2D>();
        var attack = player.GetComponent<PlayerMelee>();
        var runner = player.GetComponent<Runner>();
        var face = player.GetComponent<SpriteRenderer>();
        int completions = 0;
        room.completed.AddListener(() => completions++);

        void Place(float x)
        {
            body.position = new Vector2(x, .56f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        Require(room.Remaining == 1 && enemy.State == MeleeEnemy.Phase.Idle, "Initial encounter state");
        Place(room.ExitDoor.transform.position.x);
        float lockedUntil = Time.time + .15f;
        while (Time.time < lockedUntil) yield return null;
        Require(room.Status == CombatChallenge.Result.InProgress && completions == 0, "Locked door completed challenge");
        float startX = enemy.transform.position.x;
        Place(startX - 3);
        var gaitFrames = new System.Collections.Generic.HashSet<string>();
        int lastGaitFrame = 0;
        while (enemy.State != MeleeEnemy.Phase.Prepare)
        {
            string spriteName = enemy.GetComponent<SpriteRenderer>().sprite.name;
            if (spriteName.StartsWith("TrollWalk"))
            {
                int frame = int.Parse(spriteName.Substring("TrollWalk".Length));
                if (lastGaitFrame != 0 && frame != lastGaitFrame)
                    Require(frame == lastGaitFrame % 6 + 1, "Troll walk frame order");
                lastGaitFrame = frame;
                gaitFrames.Add(spriteName);
            }
            yield return null;
        }
        Require(gaitFrames.Count == 6, "Troll did not use all six walk frames while approaching");
        yield return null;
        Require(enemy.GetComponent<SpriteRenderer>().sprite.name == "CaveTroll", "Troll should stop walking during attack preparation");
        Debug.Log("Troll animation: six ordered frames and stationary attack preparation passed");
        Require(enemy.transform.position.x < startX - .5f, "Enemy did not approach");
        Require(player.Current == player.Maximum, "Wind-up dealt damage");
        runner.SendMessage("RequestDash", -1);
        while (enemy.State != MeleeEnemy.Phase.Recover) yield return null;
        Require(player.Current == player.Maximum && body.position.x < enemy.transform.position.x - 2, "Dash failed to evade");
        Debug.Log("Combat check: detected, approached, telegraphed and missed the dashing player");

        Place(enemy.transform.position.x + 1.1f);
        face.flipX = false;
        attack.TryAttack();
        
        while (!attack.Ready) yield return null;
        Require(enemyHP.Current == enemyHP.Maximum, "Strike damaged target behind player");
        face.flipX = true;
        for (int hits = 1; hits <= enemyHP.Maximum; hits++)
        {
            attack.TryAttack();
            attack.TryAttack();

            while (!attack.Ready) yield return null;
            Require(enemyHP.Current == enemyHP.Maximum - hits, $"Swing {hits}: HP {enemyHP.Current}, player {body.position}, enemy {enemy.transform.position}, facing {face.flipX}");
        }
        Require(room.Status == CombatChallenge.Result.InProgress && room.Remaining == 0 && completions == 0, "Enemy defeat must only unlock door");
        Place(room.ExitDoor.transform.position.x);
        while (room.Status == CombatChallenge.Result.InProgress) yield return null;
        Require(room.Status == CombatChallenge.Result.Completed && room.Remaining == 0 && completions == 1,
                "Completion must fire exactly once");
        while (enemy.gameObject.activeSelf) yield return null;
        Require(!enemy.gameObject.activeSelf, "Enemy defeat visual did not finish");
        Debug.Log("Combat check: facing, single-hit swings, defeat and completion passed");

        room.Restart();
        while (room) yield return null;
        room = UnityEngine.Object.FindAnyObjectByType<CombatChallenge>();
        player = room.Player;
        body = player.GetComponent<Rigidbody2D>();
        enemy = UnityEngine.Object.FindAnyObjectByType<MeleeEnemy>();
        Require(room.Remaining == 1 && player.Current == player.Maximum, "Restart after completion");
        Place(enemy.transform.position.x - 1.1f);
        while (enemy.State != MeleeEnemy.Phase.Recover) yield return null;
        Require(player.Current == player.Maximum - 1, "Enemy strike should hit once");
        while (room.Status == CombatChallenge.Result.InProgress) yield return null;
        Require(room.Status == CombatChallenge.Result.Failed && !player.gameObject.activeSelf && room.Remaining == 1,
                "Lethal enemy attack did not end the challenge");
        room.Restart();
        while (room) yield return null;
        room = UnityEngine.Object.FindAnyObjectByType<CombatChallenge>();
        Require(room.Status == CombatChallenge.Result.InProgress && room.Remaining == 1 &&
                room.Player.Current == room.Player.Maximum && room.RequiredEnemies[0].Alive, "Restart after death");
        Debug.Log("Combat check: lethal hits and restart after both outcomes passed");

        UnityEngine.SceneManagement.SceneManager.LoadScene("PeacefulTest");
        while (room) yield return null;
        var hud = UnityEngine.Object.FindAnyObjectByType<PlayerHUD>();
        player = hud.GetComponentInChildren<CombatHealth>();
        attack = player.GetComponent<PlayerMelee>();
        Require(player.Current == player.Maximum && attack.Ready, "Peaceful player initial state");
        attack.TryAttack();
        Require(player.transform.Find("Strike area").GetComponent<SpriteRenderer>().enabled, "Peaceful swing invisible");
        while (!attack.Ready) yield return null;
        Require(player.Current == player.Maximum, "Player hit self");
        player.TakeHit(1);
        Require(player.Current == player.Maximum - 1, "Peaceful damage");
        player.TakeHit(player.Maximum);
        Require(!player.gameObject.activeSelf && hud.isActiveAndEnabled, "Peaceful defeat");
        hud.Restart();
        while (hud) yield return null;
        player = UnityEngine.Object.FindAnyObjectByType<PlayerMelee>().GetComponent<CombatHealth>();
        Require(player.Current == player.Maximum, "Peaceful restart");
        Debug.Log("Shared player: peaceful attack, damage, death and restart passed");
        runner = player.GetComponent<Runner>();
        body = player.GetComponent<Rigidbody2D>();
        float settle = Time.time + .5f;
        while (Time.time < settle) yield return null;
        typeof(Runner).GetField("jump", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(runner, true);
        settle = Time.time + .1f;
        while (Time.time < settle) yield return null;
        Require(player.GetComponent<SpriteRenderer>().sprite.name == "HoodJump", "Jump animation missing");
        settle = Time.time + 1;
        while (Time.time < settle) yield return null;
        runner.SendMessage("RequestDash", 1);
        settle = Time.time + .06f;
        while (Time.time < settle) yield return null;
        Require(player.GetComponent<SpriteRenderer>().sprite.name.StartsWith("HoodWalk"), "Movement animation missing");
        Debug.Log("Hooded player animation states verified");
    }
}

