using UnityEngine;

public sealed class PeacefulHUD : MonoBehaviour
{
    [SerializeField] PeacefulChallenge challenge;
    [SerializeField] PressurePlate plate;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) challenge.Restart();
    }
    void OnGUI()
    {
        if (!challenge.Completed) return;
        float scale = PixelUI.Begin();
        GUILayout.BeginArea(new Rect((Screen.width / scale - 330) / 2, 175, 330, 100), GUI.skin.box);
        GUILayout.Label("Испытание пройдено", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
        if (GUILayout.Button("Повторить", GUILayout.Height(36))) challenge.Restart();
        GUILayout.EndArea();
    }
}
