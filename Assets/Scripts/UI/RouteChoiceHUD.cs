using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RouteChoiceHUD : MonoBehaviour
{
    RouteChoiceModule module;
    void Awake() => module = GetComponent<RouteChoiceModule>();
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene("MainMenu");
        if (Input.GetKeyDown(KeyCode.R)) module.Restart();
        if (Input.GetKeyDown(KeyCode.Alpha1)) module.SelectRoute(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) module.SelectRoute(1);
    }
    void OnGUI()
    {
        float scale = PixelUI.Begin();
        float width = Screen.width / scale;
        if (module.SelectedRoute < 0)
        {
            GUILayout.BeginArea(new Rect((width - 580) / 2, 130, 580, 180), GUI.skin.box);
            GUILayout.Label("Выбери путь к общему выходу");
            GUILayout.Space(15);
            if (GUILayout.Button($"Бой — победить тролля — {module.CombatCoins} монет", GUILayout.Height(45))) module.SelectRoute(0);
            if (GUILayout.Button($"Мирный путь — головоломка — {module.PeacefulCoins} монет", GUILayout.Height(45))) module.SelectRoute(1);
            GUILayout.EndArea();
        }
        else if (module.Exited)
        {
            GUILayout.BeginArea(new Rect((width - 330) / 2, 175, 330, 100), GUI.skin.box);
            GUILayout.Label("Модуль пройден");
            if (GUILayout.Button("Повторить", GUILayout.Height(36))) module.Restart();
            GUILayout.EndArea();
        }
    }
}
