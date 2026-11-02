using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    void Damage(float amount, Vector3 from);
}

public enum ItemType { Wood, Stick, Stone, Berry, RawMeat, CookedMeat }

public static class GameState
{
    public static bool Paused;
    public static bool Dead;
    public static bool Won;
    public static bool Frozen { get { return Paused || Dead || Won; } }

    public static void Reset()
    {
        Paused = false; Dead = false; Won = false;
        Time.timeScale = 1f;
    }
}

public static class ItemInfo
{
    public static string Name(ItemType t)
    {
        switch (t)
        {
            case ItemType.Wood: return "Drvo";
            case ItemType.Stick: return "Štap";
            case ItemType.Stone: return "Kamen";
            case ItemType.Berry: return "Bobice";
            case ItemType.RawMeat: return "Sirovo meso";
            case ItemType.CookedMeat: return "Pečeno meso";
        }
        return t.ToString();
    }
}

public class Inventory : MonoBehaviour
{
    readonly Dictionary<ItemType, int> items = new Dictionary<ItemType, int>();
    public bool stoneAxe;

    public int Count(ItemType t) { int n; return items.TryGetValue(t, out n) ? n : 0; }
    public bool Has(ItemType t, int n) { return Count(t) >= n; }

    public void Add(ItemType t, int n = 1)
    {
        items[t] = Count(t) + n;
    }

    public bool Remove(ItemType t, int n = 1)
    {
        if (!Has(t, n)) return false;
        items[t] = Count(t) - n;
        return true;
    }
}

// Jednostavne materijale pravimo u kodu (bez asseta), uz GPU instancing.
public static class Mats
{
    static readonly Dictionary<long, Material> cache = new Dictionary<long, Material>();

    static long Key(Color c, float s, bool unlit)
    {
        long r = Mathf.RoundToInt(c.r * 255f), g = Mathf.RoundToInt(c.g * 255f), b = Mathf.RoundToInt(c.b * 255f);
        long sm = Mathf.RoundToInt(s * 100f);
        return (((r * 256L + g) * 256L + b) * 101L + sm) * 2L + (unlit ? 1L : 0L);
    }

    public static Material Make(Color c, float smoothness = 0.1f, bool unlit = false)
    {
        long k = Key(c, smoothness, unlit);
        Material m;
        if (cache.TryGetValue(k, out m) && m != null) return m;

        Shader sh = Shader.Find(unlit ? "Unlit/Color" : "Standard");
        m = new Material(sh);
        m.color = c;
        if (!unlit)
        {
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", 0f);
        }
        m.enableInstancing = true;
        cache[k] = m;
        return m;
    }

    public static void Clear() { cache.Clear(); }
}

public static class Prim
{
    public static GameObject Cone(Transform parent, Vector3 localPos, Vector3 localScale, Color color, float smoothness = 0.05f)
    {
        var go = new GameObject("Cone");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.AddComponent<MeshFilter>().sharedMesh = MeshUtil.Cone();
        go.AddComponent<MeshRenderer>().sharedMaterial = Mats.Make(color, smoothness, false);
        return go;
    }

    // Pravi primitivu, bez kolajdera (osim ako collider = true), sa bojom.
    public static GameObject Make(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale,
                                  Color color, bool collider = false, float smoothness = 0.1f, bool unlit = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = Mats.Make(color, smoothness, unlit);
        if (!collider)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
        }
        return go;
    }
}

// Procedurni konus (za borove). Poluprecnik 0.5, visina 1, osnova na y = 0.
public static class MeshUtil
{
    static Mesh cone;

    public static Mesh Cone()
    {
        if (cone != null) return cone;
        const int seg = 12;
        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var t = new List<int>();
        Vector3 apex = new Vector3(0f, 1f, 0f);

        for (int i = 0; i < seg; i++)
        {
            float a0 = i * Mathf.PI * 2f / seg;
            float a1 = (i + 1) * Mathf.PI * 2f / seg;
            float am = (a0 + a1) * 0.5f;
            Vector3 b0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
            Vector3 b1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
            Vector3 nm = new Vector3(Mathf.Cos(am), 0.5f, Mathf.Sin(am)).normalized;

            int s = v.Count;
            v.Add(apex); v.Add(b0); v.Add(b1);
            n.Add(nm); n.Add(nm); n.Add(nm);
            t.Add(s); t.Add(s + 1); t.Add(s + 2);

            // donja strana
            int c = v.Count;
            v.Add(Vector3.zero); v.Add(b1); v.Add(b0);
            n.Add(Vector3.down); n.Add(Vector3.down); n.Add(Vector3.down);
            t.Add(c); t.Add(c + 1); t.Add(c + 2);
        }

        cone = new Mesh { name = "Cone" };
        cone.SetVertices(v);
        cone.SetNormals(n);
        cone.SetTriangles(t, 0);
        cone.RecalculateBounds();
        return cone;
    }
}
