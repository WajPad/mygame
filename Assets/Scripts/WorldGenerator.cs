using UnityEngine;

public static class World
{
    public static Terrain terrain;
    public static float size = 400f;
    public static float waterLevel = 4f;
    public static Vector3 spawn;

    public static float GroundY(float x, float z)
    {
        if (terrain == null) return 0f;
        return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
    }
}

// Pravi ostrvo: teren, voda, drveće, kamenje, štapovi, bobice, olupina aviona.
public class WorldGenerator : MonoBehaviour
{
    public int resolution = 257;
    public float maxHeight = 45f;
    public int treeCount = 900;
    public int rockCount = 110;
    public int stickCount = 160;
    public int stoneCount = 80;
    public int bushCount = 45;
    public int seed = 20260211;

    TerrainData td;
    Transform treeRoot, propRoot;

    public void Generate(Terrain terrain)
    {
        Random.InitState(seed);
        World.terrain = terrain;
        BuildTerrain(terrain);
        BuildWater();

        treeRoot = new GameObject("Trees").transform;
        propRoot = new GameObject("Props").transform;

        FindSpawn();
        Scatter();
        BuildWreck();
    }

    // ------------------------------------------------------------ teren
    void BuildTerrain(Terrain terrain)
    {
        float size = World.size;
        td = new TerrainData();
        td.heightmapResolution = resolution;
        td.size = new Vector3(size, maxHeight, size);

        float[,] h = new float[resolution, resolution];
        float ox = Random.value * 500f, oy = Random.value * 500f;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float nx = (float)x / (resolution - 1);
                float ny = (float)y / (resolution - 1);

                float n = Mathf.PerlinNoise(ox + nx * 3.5f, oy + ny * 3.5f) * 0.60f
                        + Mathf.PerlinNoise(ox + nx * 9f, oy + ny * 9f) * 0.28f
                        + Mathf.PerlinNoise(ox + nx * 24f, oy + ny * 24f) * 0.12f;

                float dx = nx - 0.5f, dy = ny - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;           // 0 centar -> 1 ivica
                float island = Mathf.Clamp01(1f - Mathf.Pow(d, 2.2f));  // ostrvo
                float v = Mathf.Lerp(0.02f, 0.62f, n) * island;
                // plaža: spljošti oko nivoa vode
                h[y, x] = Mathf.Clamp01(v);
            }
        }
        td.SetHeights(0, 0, h);

        // Slojevi (pesak, trava, stena, zemlja)
        var layers = new TerrainLayer[4];
        layers[0] = MakeLayer(new Color(0.78f, 0.72f, 0.52f), new Color(0.70f, 0.64f, 0.44f), 0.09f);
        layers[1] = MakeLayer(new Color(0.20f, 0.42f, 0.16f), new Color(0.28f, 0.50f, 0.20f), 0.07f);
        layers[2] = MakeLayer(new Color(0.42f, 0.42f, 0.44f), new Color(0.55f, 0.55f, 0.57f), 0.12f);
        layers[3] = MakeLayer(new Color(0.30f, 0.23f, 0.14f), new Color(0.38f, 0.29f, 0.18f), 0.10f);
        td.terrainLayers = layers;

        int res = 256;
        td.alphamapResolution = res;
        float[,,] a = new float[res, res, 4];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);
                float ny = (float)y / (res - 1);
                float hm = td.GetInterpolatedHeight(nx, ny);
                float steep = td.GetSteepness(nx, ny);

                float sand = Mathf.Clamp01((World.waterLevel + 1.8f - hm) / 1.4f);
                float rock = Mathf.Clamp01((steep - 26f) / 10f) + Mathf.Clamp01((hm - 26f) / 6f);
                rock = Mathf.Clamp01(rock);
                float dirt = Mathf.PerlinNoise(nx * 40f + 7f, ny * 40f + 3f) > 0.62f ? 0.7f : 0f;
                float grass = Mathf.Max(0.05f, 1f - sand - rock - dirt);

                float sum = sand + grass + rock + dirt;
                a[y, x, 0] = sand / sum;
                a[y, x, 1] = grass / sum;
                a[y, x, 2] = rock / sum;
                a[y, x, 3] = dirt / sum;
            }
        }
        td.SetAlphamaps(0, 0, a);

        terrain.terrainData = td;
        var col = terrain.GetComponent<TerrainCollider>();
        if (col == null) col = terrain.gameObject.AddComponent<TerrainCollider>();
        col.terrainData = td;
        terrain.transform.position = Vector3.zero;
        terrain.heightmapPixelError = 6f;
        terrain.basemapDistance = 300f;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        terrain.Flush();
    }

    static TerrainLayer MakeLayer(Color a, Color b, float scale)
    {
        var tex = new Texture2D(128, 128, TextureFormat.RGB24, true);
        tex.wrapMode = TextureWrapMode.Repeat;
        float off = Random.value * 100f;
        for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                float n = Mathf.PerlinNoise(off + x * scale, off + y * scale) * 0.7f
                        + Random.value * 0.3f;
                tex.SetPixel(x, y, Color.Lerp(a, b, n));
            }
        tex.Apply(true);
        var l = new TerrainLayer();
        l.diffuseTexture = tex;
        l.tileSize = new Vector2(6f, 6f);
        return l;
    }

    // ------------------------------------------------------------ voda
    void BuildWater()
    {
        var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "Water";
        water.transform.position = new Vector3(World.size * 0.5f, World.waterLevel, World.size * 0.5f);
        water.transform.localScale = Vector3.one * (World.size * 3f / 10f);
        Destroy(water.GetComponent<Collider>());
        water.GetComponent<Renderer>().sharedMaterial = Mats.Make(new Color(0.08f, 0.30f, 0.42f), 0.92f);
    }

    // ------------------------------------------------------------ pozicije
    bool RandomLandPoint(out Vector3 p, float minH, float maxSlope)
    {
        for (int i = 0; i < 12; i++)
        {
            float x = Random.Range(10f, World.size - 10f);
            float z = Random.Range(10f, World.size - 10f);
            float y = World.GroundY(x, z);
            if (y < minH) continue;
            float slope = td.GetSteepness(x / World.size, z / World.size);
            if (slope > maxSlope) continue;
            p = new Vector3(x, y, z);
            return true;
        }
        p = Vector3.zero;
        return false;
    }

    void FindSpawn()
    {
        float c = World.size * 0.5f;
        float ang = Random.Range(0f, Mathf.PI * 2f);
        Vector3 dir = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang));
        for (float r = World.size * 0.48f; r > 20f; r -= 1.5f)
        {
            float x = c + dir.x * r, z = c + dir.z * r;
            float y = World.GroundY(x, z);
            if (y > World.waterLevel + 1.2f)
            {
                World.spawn = new Vector3(x, y + 1.2f, z);
                return;
            }
        }
        World.spawn = new Vector3(c, World.GroundY(c, c) + 1.2f, c);
    }

    // ------------------------------------------------------------ rastinje i predmeti
    void Scatter()
    {
        Vector3 p;

        for (int i = 0; i < treeCount; i++)
            if (RandomLandPoint(out p, World.waterLevel + 1.2f, 32f) && (p - World.spawn).sqrMagnitude > 12f * 12f)
                MakeTree(p);

        for (int i = 0; i < rockCount; i++)
            if (RandomLandPoint(out p, World.waterLevel + 0.5f, 60f))
                MakeRock(p);

        for (int i = 0; i < stickCount; i++)
            if (RandomLandPoint(out p, World.waterLevel + 1f, 40f))
                Pickup.Spawn(ItemType.Stick, p).transform.SetParent(propRoot);

        for (int i = 0; i < stoneCount; i++)
            if (RandomLandPoint(out p, World.waterLevel + 0.8f, 50f))
                Pickup.Spawn(ItemType.Stone, p).transform.SetParent(propRoot);

        for (int i = 0; i < bushCount; i++)
            if (RandomLandPoint(out p, World.waterLevel + 1.5f, 28f))
                BerryBush.Create(p);

        // Nešto sirovina odmah kod početka
        Pickup.SpawnAround(ItemType.Stick, World.spawn, 6, 7f);
        Pickup.SpawnAround(ItemType.Stone, World.spawn, 4, 8f);
        Pickup.SpawnAround(ItemType.Wood, World.spawn, 2, 5f);
    }

    void MakeTree(Vector3 pos)
    {
        bool pine = Random.value < 0.75f;
        float s = Random.Range(0.8f, 1.5f);
        float trunkH = (pine ? 6.5f : 5.5f) * s;
        var root = new GameObject("Tree");
        root.transform.SetParent(treeRoot, false);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(Random.Range(-2f, 2f), Random.Range(0f, 360f), Random.Range(-2f, 2f));

        var trunk = Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, trunkH * 0.5f, 0),
                              new Vector3(0.55f * s, trunkH * 0.5f, 0.55f * s), new Color(0.30f, 0.20f, 0.11f), true);
        // Cilindar ima kapsulni kolajder; smanji ga da igrac lakse prolazi
        var cap = trunk.GetComponent<CapsuleCollider>();
        if (cap != null) cap.radius = 0.45f;

        if (pine)
        {
            Color g1 = Color.Lerp(new Color(0.06f, 0.20f, 0.10f), new Color(0.10f, 0.28f, 0.12f), Random.value);
            float[] baseY = { 0.28f, 0.46f, 0.64f, 0.82f };
            float[] diam = { 4.4f, 3.6f, 2.8f, 1.9f };
            float[] hgt = { 2.8f, 2.5f, 2.2f, 1.9f };
            for (int i = 0; i < 4; i++)
            {
                Color g = Color.Lerp(g1, new Color(0.14f, 0.34f, 0.15f), i * 0.12f);
                Prim.Cone(root.transform, new Vector3(0, trunkH * baseY[i], 0),
                          new Vector3(diam[i] * s, hgt[i] * s, diam[i] * s), g);
            }
        }
        else
        {
            Color green = Color.Lerp(new Color(0.12f, 0.32f, 0.12f), new Color(0.20f, 0.42f, 0.14f), Random.value);
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, trunkH * 0.95f, 0), new Vector3(3.4f * s, 2.6f * s, 3.4f * s), green);
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, trunkH * 1.25f, 0), new Vector3(2.6f * s, 2.2f * s, 2.6f * s), green);
            Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, trunkH * 1.5f, 0), new Vector3(1.7f * s, 1.6f * s, 1.7f * s), green);
        }

        root.AddComponent<TreeObj>();
    }

    void MakeRock(Vector3 pos)
    {
        float s = Random.Range(0.7f, 2.4f);
        var rock = Prim.Make(PrimitiveType.Sphere, propRoot, pos + Vector3.up * (0.3f * s),
                             new Vector3(1.6f * s, 1.0f * s, 1.3f * s),
                             Color.Lerp(new Color(0.38f, 0.38f, 0.4f), new Color(0.55f, 0.54f, 0.52f), Random.value), true, 0.15f);
        rock.transform.rotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
    }

    // Olupina aviona kod početne tačke
    void BuildWreck()
    {
        Vector3 p = World.spawn + new Vector3(7f, 0, 5f);
        p.y = World.GroundY(p.x, p.z);
        var root = new GameObject("PlaneWreck").transform;
        root.SetParent(propRoot, false);
        root.position = p;
        root.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

        Color metal = new Color(0.72f, 0.74f, 0.78f);
        Color burnt = new Color(0.18f, 0.17f, 0.16f);
        var body = Prim.Make(PrimitiveType.Cylinder, root, new Vector3(0, 1.1f, 0), new Vector3(2.0f, 3.2f, 2.0f), metal, true, 0.6f);
        body.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        var wing = Prim.Make(PrimitiveType.Cube, root, new Vector3(0, 1.2f, -0.5f), new Vector3(0.25f, 0.12f, 9f), metal, true, 0.6f);
        wing.transform.localRotation = Quaternion.Euler(8f, 10f, 4f);
        var tail = Prim.Make(PrimitiveType.Cube, root, new Vector3(-3.4f, 2.0f, 0), new Vector3(0.2f, 1.8f, 0.9f), burnt, true, 0.3f);
        tail.transform.localRotation = Quaternion.Euler(0, 0, -12f);
        Prim.Make(PrimitiveType.Cube, root, new Vector3(3.3f, 0.5f, 0.6f), new Vector3(1.2f, 0.8f, 1.2f), burnt, true, 0.3f);
    }

    public Vector3 RandomSpawnNear(Vector3 center, float minR, float maxR)
    {
        for (int i = 0; i < 20; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(minR, maxR);
            float x = Mathf.Clamp(center.x + Mathf.Cos(a) * r, 8f, World.size - 8f);
            float z = Mathf.Clamp(center.z + Mathf.Sin(a) * r, 8f, World.size - 8f);
            if (World.GroundY(x, z) > World.waterLevel + 0.8f)
                return new Vector3(x, 0, z);
        }
        return Vector3.zero;
    }
}
