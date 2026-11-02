using UnityEngine;

// Jedina komponenta u sceni. Pri startu pravi ceo svet, igrača, svetlo, HUD...
public class GameBootstrap : MonoBehaviour
{
    WorldGenerator gen;
    PlayerController player;
    float nextSpawn = 8f;
    float nextAnimalCheck = 10f;

    void Start()
    {
        GameState.Reset();
        Mats.Clear();
        Campfire.All.Clear();
        Enemy.All.Clear();
        Animal.Count = 0;

        QualitySettings.shadowDistance = 90f;
        QualitySettings.shadows = ShadowQuality.All;

        // Svetla
        var sunGo = new GameObject("Sun");
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;

        var moonGo = new GameObject("Moon");
        var moon = moonGo.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = new Color(0.5f, 0.6f, 1f);
        moon.shadows = LightShadows.None;

        var dn = gameObject.AddComponent<DayNightCycle>();
        dn.sun = sun;
        dn.moon = moon;

        // Svet
        var terrain = FindObjectOfType<Terrain>();
        if (terrain == null)
        {
            var td = new TerrainData();
            terrain = Terrain.CreateTerrainGameObject(td).GetComponent<Terrain>();
        }
        gen = gameObject.AddComponent<WorldGenerator>();
        gen.Generate(terrain);

        // Igrač
        var pg = new GameObject("Player");
        pg.transform.position = World.spawn;
        player = pg.AddComponent<PlayerController>();
        player.BuildCamera();
        Vector3 toCenter = new Vector3(World.size * 0.5f, 0, World.size * 0.5f) - World.spawn;
        player.SetYaw(Mathf.Atan2(toCenter.x, toCenter.z) * Mathf.Rad2Deg);

        var hud = gameObject.AddComponent<HUD>();
        hud.player = player;

        // Zečevi
        for (int i = 0; i < 28; i++) SpawnAnimal(0f, World.size * 0.45f, World.spawn);
    }

    void SpawnAnimal(float minR, float maxR, Vector3 around)
    {
        Vector3 c = new Vector3(World.size * 0.5f, 0, World.size * 0.5f);
        Vector3 p = gen.RandomSpawnNear(c, minR, maxR);
        if (p == Vector3.zero) return;
        Animal.Spawn(p, player.transform);
    }

    void Update()
    {
        if (GameState.Frozen || player == null) return;
        var dn = DayNightCycle.I;

        // Mutanti noću
        if (Time.time >= nextSpawn)
        {
            nextSpawn = Time.time + 5f;
            int cap = dn.IsNight ? Mathf.Min(2 + dn.day * 2, 14) : 0;
            if (Enemy.All.Count < cap)
            {
                Vector3 p = gen.RandomSpawnNear(player.transform.position, 35f, 60f);
                if (p != Vector3.zero && Campfire.Nearest(p, 14f) == null)
                    Enemy.Spawn(p, player.transform, player.stats);
            }
        }

        // Dopuna zečeva
        if (Time.time >= nextAnimalCheck)
        {
            nextAnimalCheck = Time.time + 20f;
            if (Animal.Count < 18) SpawnAnimal(0f, World.size * 0.4f, player.transform.position);
        }
    }
}
