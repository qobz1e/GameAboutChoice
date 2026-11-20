using UnityEngine;
using UnityEngine.SceneManagement;

public class PrototypeMenu : MonoBehaviour
{
    const string DoubleTapKey = "Controls.DoubleTapDash";
    public static bool DoubleTapEnabled => PlayerPrefs.GetInt(DoubleTapKey, 1) != 0;
    bool settings;
    bool AtMenu => SceneManager.GetActiveScene().name == "MainMenu";

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (AtMenu) settings = false;
        else SceneManager.LoadScene("MainMenu");
    }

    void OnGUI()
    {
        if (!AtMenu) return;
        float scale = PixelUI.Begin();
        float width = Screen.width / scale;
        if (settings)
        {
            GUILayout.BeginArea(new Rect(width / 2 - 320, 30, 640, 430), GUI.skin.box);
            GUILayout.Label("НАСТРОЙКИ / УПРАВЛЕНИЕ");
            GUILayout.Space(14);
            GUILayout.Label("A / D — идти и толкать ящик с любой стороны");
            GUILayout.Space(10);
            GUILayout.Label("W / Пробел — прыжок и отскок от стены");
            GUILayout.Space(10);
            GUILayout.Label("Левый Shift — дэш по направлению взгляда");
            GUILayout.Space(10);
            GUILayout.Label("S — ускорить падение    J — удар");
            GUILayout.Space(10);
            GUILayout.Label("R — начать комнату заново    Esc — главное меню");
            GUILayout.Space(10);
            GUILayout.Label("E — переключить ближайший рычаг");
            GUILayout.Space(10);
            GUILayout.Label("1 / 2 — выбрать боевой / мирный путь");
            GUILayout.Space(20);
            bool enabled = GUILayout.Toggle(DoubleTapEnabled, "Дэш по двойному нажатию A или D");
            if (enabled != DoubleTapEnabled)
            {
                PlayerPrefs.SetInt(DoubleTapKey, enabled ? 1 : 0);
                PlayerPrefs.Save();
            }
            GUILayout.Space(10);
            GUILayout.Label("Два нажатия одной клавиши за 0,24 секунды.");
            GUILayout.Space(20);
            if (GUILayout.Button("Назад", GUILayout.Height(35))) settings = false;
            GUILayout.EndArea();
            return;
        }
        GUILayout.BeginArea(new Rect(width / 2 - 180, 85, 360, 280), GUI.skin.box);
        GUILayout.Label("DIPLOMA GAME", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
        GUILayout.Space(18);
        if (GUILayout.Button("Мирная комната", GUILayout.Height(40))) SceneManager.LoadScene("PeacefulTest");
        if (GUILayout.Button("Боевая комната", GUILayout.Height(40))) SceneManager.LoadScene("CombatTest");
        if (GUILayout.Button("Выбор пути", GUILayout.Height(40))) SceneManager.LoadScene("RouteChoice");
        if (GUILayout.Button("Настройки", GUILayout.Height(40))) settings = true;
        GUILayout.EndArea();
    }
}

