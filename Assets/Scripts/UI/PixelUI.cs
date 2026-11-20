using UnityEngine;

public static class PixelUI
{
    static Font font;
    public static float Begin()
    {
        if (!font) font = Resources.Load<Font>("Fonts/PressStart2P");
        GUI.skin.font = font;
        GUI.skin.label.fontSize = 10;
        GUI.skin.label.wordWrap = true;
        GUI.skin.button.fontSize = 10;
        GUI.skin.box.fontSize = 10;
        GUI.skin.toggle.fontSize = 10;
        float scale = Mathf.Min(Screen.width / 800f, Screen.height / 450f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        return scale;
    }
}
