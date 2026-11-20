using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PlayerHUD : MonoBehaviour
{
    [SerializeField] CombatHealth player;
    void OnEnable() => player.Defeated += OnDefeated;
    void OnDisable() => player.Defeated -= OnDefeated;
    void OnDefeated() => player.gameObject.SetActive(false);
    void Update()
    {
        if (!player.Alive && Input.GetKeyDown(KeyCode.R)) Restart();
    }
    void OnGUI()
    {
        float scale = PixelUI.Begin();
        var purse = player.GetComponent<CoinPurse>();
        GUI.Box(new Rect(14, 14, 160, 32), $"HP {player.Current} / {player.Maximum}");
        GUI.Box(new Rect(Screen.width / scale - 194, 14, 180, 32), $"Монеты {purse.Balance}");
        if (player.Alive) return;
        GUILayout.BeginArea(new Rect((Screen.width / scale - 330) / 2, 175, 330, 100), GUI.skin.box);
        GUILayout.Label("Персонаж погиб", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        if (GUILayout.Button("Повторить", GUILayout.Height(36))) Restart();
        GUILayout.EndArea();
    }
    public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
