using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle I;

    public float hour = 8f;
    public float dayLengthSeconds = 240f;
    public int day = 1;
    public Light sun;
    public Light moon;

    static readonly Color DayAmbient = new Color(0.62f, 0.68f, 0.76f);
    static readonly Color NightAmbient = new Color(0.05f, 0.06f, 0.11f);
    static readonly Color DayFog = new Color(0.68f, 0.78f, 0.88f);
    static readonly Color DuskFog = new Color(0.55f, 0.35f, 0.28f);
    static readonly Color NightFog = new Color(0.02f, 0.03f, 0.06f);

    public bool IsNight { get { return hour >= 19f || hour < 5.5f; } }

    void Awake()
    {
        I = this;
        RenderSettings.skybox = new Material(Shader.Find("Skybox/Procedural"));
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.sun = sun;
    }

    void Update()
    {
        if (!GameState.Frozen)
        {
            hour += 24f * Time.deltaTime / dayLengthSeconds;
            if (hour >= 24f) { hour -= 24f; day++; }
        }
        Apply();
    }

    public void SkipToMorning()
    {
        if (hour >= 12f) day++;
        hour = 6.5f;
    }

    void Apply()
    {
        float angle = (hour - 6f) / 24f * 360f;
        sun.transform.rotation = Quaternion.Euler(angle, 170f, 0f);
        moon.transform.rotation = Quaternion.Euler(angle + 180f, 170f, 0f);

        float h = Mathf.Sin((hour - 6f) / 24f * Mathf.PI * 2f); // 1 u podne, -1 u ponoc
        sun.intensity = Mathf.Clamp01(h * 3f) * 1.2f;
        moon.intensity = Mathf.Clamp01(-h * 3f) * 0.3f;

        float t = Mathf.Clamp01(h * 1.6f + 0.35f);
        RenderSettings.ambientLight = Color.Lerp(NightAmbient, DayAmbient, t);

        float dusk = 1f - Mathf.Abs(h) * 3f; // jako blizu horizonta
        Color fog = Color.Lerp(NightFog, DayFog, t);
        if (dusk > 0f) fog = Color.Lerp(fog, DuskFog, Mathf.Clamp01(dusk) * 0.6f);
        RenderSettings.fogColor = fog;
        RenderSettings.fogDensity = Mathf.Lerp(0.012f, 0.0045f, t);

        if (RenderSettings.skybox != null)
            RenderSettings.skybox.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.6f, 1.0f, t));
    }

    public string Clock()
    {
        int hh = Mathf.FloorToInt(hour);
        int mm = Mathf.FloorToInt((hour - hh) * 60f);
        return hh.ToString("00") + ":" + mm.ToString("00");
    }
}
