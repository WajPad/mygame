using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    string GetHint(PlayerController p);
    void Interact(PlayerController p);
}

// ---------------------------------------------------------------- Pickup
public class Pickup : MonoBehaviour, IInteractable
{
    public ItemType type;
    public int amount = 1;

    public static Pickup Spawn(ItemType type, Vector3 pos, int amount = 1)
    {
        pos.y = World.GroundY(pos.x, pos.z) + 0.12f;
        var root = new GameObject("Pickup_" + type);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        switch (type)
        {
            case ItemType.Wood:
                Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.12f, 0), new Vector3(0.28f, 0.55f, 0.28f),
                          new Color(0.40f, 0.26f, 0.13f)).transform.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            case ItemType.Stick:
                Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.05f, 0), new Vector3(0.07f, 0.45f, 0.07f),
                          new Color(0.50f, 0.36f, 0.20f)).transform.localRotation = Quaternion.Euler(0, 0, 90);
                break;
            case ItemType.Stone:
                Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.07f, 0), new Vector3(0.32f, 0.22f, 0.28f),
                          new Color(0.50f, 0.50f, 0.52f), false, 0.2f);
                break;
            case ItemType.Berry:
                Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.1f, 0), new Vector3(0.22f, 0.22f, 0.22f),
                          new Color(0.75f, 0.1f, 0.2f), false, 0.5f);
                break;
            case ItemType.RawMeat:
                Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.07f, 0), new Vector3(0.32f, 0.14f, 0.22f),
                          new Color(0.85f, 0.40f, 0.42f), false, 0.4f);
                break;
            case ItemType.CookedMeat:
                Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.07f, 0), new Vector3(0.32f, 0.14f, 0.22f),
                          new Color(0.45f, 0.25f, 0.12f), false, 0.3f);
                break;
        }

        var col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0, 0.15f, 0);
        col.size = new Vector3(0.7f, 0.5f, 0.7f);

        var p = root.AddComponent<Pickup>();
        p.type = type;
        p.amount = amount;
        return p;
    }

    public static void SpawnAround(ItemType type, Vector3 center, int count, float radius)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 r = Random.insideUnitCircle * radius;
            Spawn(type, center + new Vector3(r.x, 0, r.y));
        }
    }

    public string GetHint(PlayerController p) { return "E: uzmi " + ItemInfo.Name(type); }

    public void Interact(PlayerController p)
    {
        p.inv.Add(type, amount);
        HUD.Notify("+" + amount + " " + ItemInfo.Name(type));
        Destroy(gameObject);
    }
}

// ---------------------------------------------------------------- Drvo
public class TreeObj : MonoBehaviour, IDamageable
{
    public float hp = 100f;
    float shake;
    Quaternion baseRot;

    void Start() { baseRot = transform.rotation; }

    public void Damage(float amount, Vector3 from)
    {
        hp -= amount;
        shake = 0.3f;
        if (hp <= 0f) Fell();
    }

    void Update()
    {
        if (shake > 0f)
        {
            shake -= Time.deltaTime;
            float s = Mathf.Sin(Time.time * 70f) * Mathf.Max(0f, shake) * 6f;
            transform.rotation = baseRot * Quaternion.Euler(s, 0f, s * 0.6f);
        }
    }

    void Fell()
    {
        Vector3 c = transform.position;
        Pickup.SpawnAround(ItemType.Wood, c, 3, 1.4f);
        Pickup.SpawnAround(ItemType.Stick, c, Random.Range(1, 4), 1.8f);
        HUD.Notify("Drvo je oboreno!");
        Destroy(gameObject);
    }
}

// ---------------------------------------------------------------- Zbun sa bobicama
public class BerryBush : MonoBehaviour, IInteractable
{
    public int berries = 4;
    const int Max = 4;
    float regrow;
    readonly List<GameObject> fruit = new List<GameObject>();

    public static void Create(Vector3 pos)
    {
        pos.y = World.GroundY(pos.x, pos.z);
        var root = new GameObject("BerryBush");
        root.transform.position = pos;
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.4f, 0), new Vector3(1.3f, 0.9f, 1.3f),
                  new Color(0.16f, 0.36f, 0.14f), true);
        var b = root.AddComponent<BerryBush>();
        for (int i = 0; i < Max; i++)
        {
            float a = i * Mathf.PI * 2f / Max + 0.4f;
            var f = Prim.Make(PrimitiveType.Sphere, root.transform,
                              new Vector3(Mathf.Cos(a) * 0.55f, 0.45f + (i % 2) * 0.2f, Mathf.Sin(a) * 0.55f),
                              Vector3.one * 0.16f, new Color(0.8f, 0.08f, 0.18f), false, 0.5f);
            b.fruit.Add(f);
        }
    }

    void Update()
    {
        if (!GameState.Frozen && berries < Max)
        {
            regrow += Time.deltaTime;
            if (regrow > 60f) { regrow = 0f; berries++; }
        }
        for (int i = 0; i < fruit.Count; i++)
            if (fruit[i] != null) fruit[i].SetActive(i < berries);
    }

    public string GetHint(PlayerController p)
    {
        return berries > 0 ? "E: beri bobice (" + berries + ")" : "Nema zrelih bobica";
    }

    public void Interact(PlayerController p)
    {
        if (berries <= 0) return;
        berries--;
        p.inv.Add(ItemType.Berry);
        HUD.Notify("+1 Bobice");
    }
}

// ---------------------------------------------------------------- Logorska vatra
public class Campfire : MonoBehaviour, IInteractable
{
    public static readonly List<Campfire> All = new List<Campfire>();

    public float fuel = 150f;
    Light lt;
    GameObject flames;
    Transform flameA, flameB;

    public bool Burning { get { return fuel > 0f; } }

    public static Campfire Create(Vector3 pos)
    {
        pos.y = World.GroundY(pos.x, pos.z);
        var root = new GameObject("Campfire");
        root.transform.position = pos;

        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f;
            Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
            var log = Prim.Make(PrimitiveType.Cylinder, root.transform, dir * 0.18f + new Vector3(0, 0.12f, 0),
                                new Vector3(0.16f, 0.5f, 0.16f), new Color(0.35f, 0.22f, 0.11f));
            log.transform.localRotation = Quaternion.Euler(90f, a + 90f, 0f);
        }
        for (int i = 0; i < 7; i++)
        {
            Vector3 dir = Quaternion.Euler(0, i * 360f / 7f, 0) * Vector3.forward;
            Prim.Make(PrimitiveType.Sphere, root.transform, dir * 0.6f + new Vector3(0, 0.1f, 0),
                      new Vector3(0.25f, 0.2f, 0.25f), new Color(0.45f, 0.45f, 0.47f), false, 0.2f);
        }

        var cc = root.AddComponent<CapsuleCollider>();
        cc.radius = 0.5f; cc.height = 0.6f; cc.center = new Vector3(0, 0.3f, 0);

        var c = root.AddComponent<Campfire>();
        c.flames = new GameObject("Flames");
        c.flames.transform.SetParent(root.transform, false);
        c.flameA = Prim.Make(PrimitiveType.Sphere, c.flames.transform, new Vector3(0, 0.4f, 0),
                             new Vector3(0.4f, 0.65f, 0.4f), new Color(1f, 0.45f, 0.05f), false, 0f, true).transform;
        c.flameB = Prim.Make(PrimitiveType.Sphere, c.flames.transform, new Vector3(0, 0.35f, 0),
                             new Vector3(0.22f, 0.4f, 0.22f), new Color(1f, 0.85f, 0.25f), false, 0f, true).transform;

        var lg = new GameObject("FireLight");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 0.8f, 0);
        c.lt = lg.AddComponent<Light>();
        c.lt.type = LightType.Point;
        c.lt.color = new Color(1f, 0.62f, 0.28f);
        c.lt.range = 18f;
        c.lt.shadows = LightShadows.None;
        return c;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        if (!GameState.Frozen && fuel > 0f) fuel -= Time.deltaTime;

        bool on = fuel > 0f;
        flames.SetActive(on);
        lt.enabled = on;
        if (on)
        {
            float n = Mathf.PerlinNoise(Time.time * 6f, transform.position.x);
            lt.intensity = 1.5f + n * 1.0f;
            float s = 0.85f + n * 0.5f;
            flameA.localScale = new Vector3(0.4f * s, 0.65f * (0.8f + n * 0.7f), 0.4f * s);
            flameB.localScale = new Vector3(0.22f * s, 0.4f * (0.7f + n), 0.22f * s);
        }
    }

    public static Campfire Nearest(Vector3 pos, float radius)
    {
        Campfire best = null;
        float bd = radius * radius;
        for (int i = 0; i < All.Count; i++)
        {
            if (!All[i].Burning) continue;
            float d = (All[i].transform.position - pos).sqrMagnitude;
            if (d < bd) { bd = d; best = All[i]; }
        }
        return best;
    }

    public string GetHint(PlayerController p)
    {
        string state = Burning ? "gori još " + Mathf.CeilToInt(fuel) + "s" : "ugasla";
        if (p.inv.Has(ItemType.Wood, 1)) return "E: dodaj drvo u vatru (" + state + ")";
        return "Vatra " + state + (Burning ? "  |  C: ispeci meso" : " (treba drvo)");
    }

    public void Interact(PlayerController p)
    {
        if (p.inv.Remove(ItemType.Wood, 1))
        {
            fuel = Mathf.Max(fuel, 0f) + 120f;
            HUD.Notify("Dodao si drvo u vatru");
        }
        else HUD.Notify("Nemaš drva!");
    }
}

// ---------------------------------------------------------------- Sklonište
public class Shelter : MonoBehaviour, IInteractable
{
    public static void Create(Vector3 pos, float yaw)
    {
        pos.y = World.GroundY(pos.x, pos.z);
        var root = new GameObject("Shelter");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0, yaw, 0);

        Color wood = new Color(0.36f, 0.24f, 0.12f);
        Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.04f, 0), new Vector3(2.4f, 0.08f, 2.2f),
                  new Color(0.22f, 0.32f, 0.14f));
        Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0, 0.8f, -1.05f), new Vector3(2.4f, 1.6f, 0.1f), wood, true);
        Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(-1.15f, 0.8f, 0.95f), new Vector3(0.12f, 0.8f, 0.12f), wood);
        Prim.Make(PrimitiveType.Cylinder, root.transform, new Vector3(1.15f, 0.8f, 0.95f), new Vector3(0.12f, 0.8f, 0.12f), wood);
        var roof = Prim.Make(PrimitiveType.Cube, root.transform, new Vector3(0, 1.55f, 0), new Vector3(2.7f, 0.1f, 2.5f),
                             new Color(0.18f, 0.34f, 0.15f), true);
        roof.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);

        var col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.center = new Vector3(0, 0.8f, 0);
        col.size = new Vector3(2.4f, 1.6f, 2.2f);
        root.AddComponent<Shelter>();
    }

    public string GetHint(PlayerController p)
    {
        return DayNightCycle.I != null && DayNightCycle.I.IsNight ? "E: spavaj do jutra" : "Možeš da spavaš samo noću";
    }

    public void Interact(PlayerController p)
    {
        var dn = DayNightCycle.I;
        if (!dn.IsNight) { HUD.Notify("Ne možeš da spavaš po danu."); return; }
        for (int i = 0; i < Enemy.All.Count; i++)
        {
            if ((Enemy.All[i].transform.position - transform.position).sqrMagnitude < 22f * 22f)
            {
                HUD.Notify("Nešto je blizu... ne možeš da zaspiš!");
                return;
            }
        }
        dn.SkipToMorning();
        p.stats.hunger = Mathf.Max(0f, p.stats.hunger - 15f);
        p.stats.thirst = Mathf.Max(0f, p.stats.thirst - 15f);
        p.stats.health = Mathf.Min(100f, p.stats.health + 25f);
        HUD.Notify("Prespavao si noć. Svanulo je.");
    }
}
