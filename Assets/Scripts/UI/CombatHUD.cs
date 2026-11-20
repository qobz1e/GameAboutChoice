using UnityEngine;

public sealed class CombatHUD : MonoBehaviour
{
    CombatChallenge room;
    void Awake() => room = GetComponent<CombatChallenge>();
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) room.Restart();
    }
    void OnGUI()
    {
        if (room.Status != CombatChallenge.Result.Completed) return;
        float scale = PixelUI.Begin();
        GUILayout.BeginArea(new Rect((Screen.width / scale - 330) / 2, 175, 330, 100), GUI.skin.box);
        GUILayout.Label("Испытание пройдено", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        if (GUILayout.Button("Повторить", GUILayout.Height(36))) room.Restart();
        GUILayout.EndArea();
    }
}
