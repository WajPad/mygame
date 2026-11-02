using System.Collections.Generic;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public static HUD I;
    public PlayerController player;

    class Note { public string text; public float until; }
    readonly List<Note> notes = new List<Note>();
    bool showHelp = true;
    GUIStyle style;

    public const int DaysToWin = 7;

    public static void Notify(string text)
    {
        if (I == null) return;
        // ne ponavljaj istu poruku uzastopno
        if (I.notes.Count > 0 && I.notes[I.notes.Count - 1].text == text) { I.notes[I.notes.Count - 1].until = Time.unscaledTime + 3.5f; return; }
        I.notes.Add(new Note { text = text, until = Time.unscaledTime + 3.5f });
        if (I.notes.Count > 6) I.notes.RemoveAt(0);
    }

    void Awake() { I = this; }

    void Start()
    {
        Notify("Preživi " + DaysToWin + " dana na ostrvu. Pritisni H za komande.");
    }

    void Update()
    {
        if (DayNightCycle.I != null && DayNightCycle.I.day > DaysToWin && !GameState.Dead && !GameState.Won)
            GameState.Won = true;

        if (Input.GetKeyDown(KeyCode.H)) showHelp = !showHelp;

        if (!GameState.Dead && !GameState.Won && Input.GetKeyDown(KeyCode.Escape))
        {
            GameState.Paused = !GameState.Paused;
            Time.timeScale = GameState.Paused ? 0f : 1f;
        }

        if (GameState.Paused && Input.GetKeyDown(KeyCode.Q)) Application.Quit();

        if ((GameState.Dead || GameState.Won) && Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        Cursor.lockState = GameState.Frozen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = GameState.Frozen;

        for (int i = notes.Count - 1; i >= 0; i--)
            if (Time.unscaledTime > notes[i].until) notes.RemoveAt(i);
    }

    // ------------------------------------------------------------ crtanje
    void Text(float x, float y, float w, float h, string t, int size, Color c, TextAnchor anchor = TextAnchor.UpperLeft, bool bold = false)
    {
        style.fontSize = size;
        style.alignment = anchor;
        style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        style.normal.textColor = new Color(0, 0, 0, 0.8f);
        GUI.Label(new Rect(x + 1.5f, y + 1.5f, w, h), t, style);
        style.normal.textColor = c;
        GUI.Label(new Rect(x, y, w, h), t, style);
    }

    void Box(float x, float y, float w, float h, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    void Bar(float x, float y, string label, float value, Color fill)
    {
        const float W = 220f, H = 18f;
        Box(x, y, W, H, new Color(0, 0, 0, 0.55f));
        Box(x + 2, y + 2, (W - 4f) * Mathf.Clamp01(value / 100f), H - 4f, fill);
        Text(x + 6, y - 1, W, H, label, 13, Color.white, TextAnchor.MiddleLeft, true);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (player == null) return;
        if (style == null) { style = new GUIStyle(GUI.skin.label); style.wordWrap = false; }

        float s = Screen.height / 720f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        float W = Screen.width / s, H = 720f;

        var st = player.stats;

        // crvenilo kad te udare
        if (st.hurtFlash > 0f) Box(0, 0, W, H, new Color(0.8f, 0f, 0f, Mathf.Clamp01(st.hurtFlash) * 0.45f));
        // slab zdravlje
        if (st.health < 30f) Box(0, 0, W, H, new Color(0.5f, 0f, 0f, (30f - st.health) / 30f * 0.25f));

        // nišan
        Box(W * 0.5f - 2f, H * 0.5f - 2f, 4f, 4f, new Color(1, 1, 1, 0.85f));

        // statistike
        Bar(20, H - 110, "ZDRAVLJE", st.health, new Color(0.85f, 0.15f, 0.15f));
        Bar(20, H - 86, "GLAD", st.hunger, new Color(0.9f, 0.55f, 0.12f));
        Bar(20, H - 62, "ŽEĐ", st.thirst, new Color(0.2f, 0.55f, 0.95f));
        Bar(20, H - 38, "IZDRŽLJIVOST", st.stamina, new Color(0.85f, 0.8f, 0.2f));

        // dan / vreme
        var dn = DayNightCycle.I;
        if (dn != null)
        {
            string phase = dn.IsNight ? "NOĆ" : "DAN";
            Color pc = dn.IsNight ? new Color(0.6f, 0.7f, 1f) : new Color(1f, 0.9f, 0.5f);
            Text(W - 260, 14, 245, 30, "Dan " + Mathf.Min(dn.day, DaysToWin) + " / " + DaysToWin, 22, Color.white, TextAnchor.UpperRight, true);
            Text(W - 260, 40, 245, 26, dn.Clock() + "  " + phase, 18, pc, TextAnchor.UpperRight, true);
        }

        // inventar
        float iy = 90f;
        Box(W - 200, iy - 6, 188, 168, new Color(0, 0, 0, 0.4f));
        Text(W - 192, iy, 180, 20, "RANAC", 14, new Color(1, 1, 1, 0.7f), TextAnchor.UpperLeft, true);
        iy += 22f;
        ItemType[] order = { ItemType.Wood, ItemType.Stick, ItemType.Stone, ItemType.Berry, ItemType.RawMeat, ItemType.CookedMeat };
        foreach (var t in order)
        {
            Text(W - 192, iy, 120, 20, ItemInfo.Name(t), 15, Color.white);
            Text(W - 80, iy, 60, 20, player.inv.Count(t).ToString(), 15, new Color(1f, 0.9f, 0.5f), TextAnchor.UpperRight, true);
            iy += 22f;
        }
        Text(W - 192, iy, 180, 20, player.inv.stoneAxe ? "Kamena sekira (jaka)" : "Obična sekira", 13, new Color(0.7f, 0.9f, 0.7f));

        // hint
        if (!string.IsNullOrEmpty(player.Hint))
            Text(W * 0.5f - 300, H * 0.5f + 26, 600, 30, player.Hint, 20, Color.white, TextAnchor.UpperCenter, true);

        // poruke
        float ny = 14f;
        for (int i = 0; i < notes.Count; i++)
        {
            Text(20, ny, 560, 24, notes[i].text, 17, new Color(1f, 1f, 1f, 0.95f), TextAnchor.UpperLeft, true);
            ny += 24f;
        }

        // pomoć
        if (showHelp)
        {
            string help =
                "KOMANDE\n" +
                "WASD kretanje   Shift trčanje   Space skok\n" +
                "Levi klik: seci / napadni   E: uzmi / koristi / pij vodu\n" +
                "F: jedi   C: ispeci meso kraj vatre\n" +
                "1: vatra (2 drva + 3 štapa)\n" +
                "2: sklonište (6 drva + 4 štapa)\n" +
                "3: kamena sekira (3 kamena + 2 štapa)\n" +
                "Voda: stani u more i pritisni E. Mutanti beže od vatre.\n" +
                "H: sakrij pomoć   Esc: pauza";
            Box(20, 120, 520, 188, new Color(0, 0, 0, 0.45f));
            Text(30, 126, 500, 180, help, 15, Color.white);
        }

        // ekrani
        if (GameState.Paused)
        {
            Box(0, 0, W, H, new Color(0, 0, 0, 0.6f));
            Text(0, H * 0.4f, W, 60, "PAUZA", 48, Color.white, TextAnchor.UpperCenter, true);
            Text(0, H * 0.4f + 64, W, 30, "Esc: nastavi    Q: izlaz", 20, Color.white, TextAnchor.UpperCenter);
        }
        else if (GameState.Dead)
        {
            Box(0, 0, W, H, new Color(0.25f, 0, 0, 0.75f));
            Text(0, H * 0.36f, W, 70, "UMRO SI", 64, new Color(1f, 0.3f, 0.3f), TextAnchor.UpperCenter, true);
            int days = dn != null ? dn.day : 1;
            Text(0, H * 0.36f + 78, W, 30, "Preživeo si " + days + (days == 1 ? " dan" : " dana") + ".", 22, Color.white, TextAnchor.UpperCenter);
            Text(0, H * 0.36f + 112, W, 30, "R: pokušaj ponovo", 20, Color.white, TextAnchor.UpperCenter);
        }
        else if (GameState.Won)
        {
            Box(0, 0, W, H, new Color(0, 0.15f, 0.05f, 0.75f));
            Text(0, H * 0.36f, W, 70, "PREŽIVEO SI!", 64, new Color(0.5f, 1f, 0.6f), TextAnchor.UpperCenter, true);
            Text(0, H * 0.36f + 78, W, 30, "Spasilačka ekipa te je pronašla posle " + DaysToWin + " dana.", 22, Color.white, TextAnchor.UpperCenter);
            Text(0, H * 0.36f + 112, W, 30, "R: igraj ponovo", 20, Color.white, TextAnchor.UpperCenter);
        }
    }
}
