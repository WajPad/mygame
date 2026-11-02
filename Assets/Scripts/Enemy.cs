using System.Collections.Generic;
using UnityEngine;

// Noćni mutant: juri igrača, beži od vatre.
[RequireComponent(typeof(CharacterController))]
public class Enemy : MonoBehaviour, IDamageable
{
    public static readonly List<Enemy> All = new List<Enemy>();

    public float hp = 60f;
    public float walkSpeed = 1.8f;
    public float chaseSpeed = 4.4f;
    public float attackRange = 1.9f;
    public float attackDamage = 12f;
    public float attackCooldown = 1.3f;

    CharacterController cc;
    Transform player;
    PlayerStats stats;
    float nextAttack;
    Vector3 wanderDir;
    float wanderUntil;
    Vector3 knock;
    float lungeAnim;
    Transform armL, armR;

    public static Enemy Spawn(Vector3 pos, Transform player, PlayerStats stats)
    {
        pos.y = World.GroundY(pos.x, pos.z) + 0.1f;
        var root = new GameObject("Mutant");
        root.transform.position = pos;

        Color skin = new Color(0.62f, 0.66f, 0.55f);
        Color dark = new Color(0.25f, 0.22f, 0.2f);
        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0, 0.95f, 0), new Vector3(0.65f, 0.85f, 0.5f), skin);
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 1.85f, 0.05f), new Vector3(0.38f, 0.42f, 0.38f), skin);
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(-0.09f, 1.9f, 0.22f), Vector3.one * 0.07f, new Color(1f, 0.1f, 0.05f), false, 0f, true);
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0.09f, 1.9f, 0.22f), Vector3.one * 0.07f, new Color(1f, 0.1f, 0.05f), false, 0f, true);
        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(-0.2f, 0.3f, 0), new Vector3(0.22f, 0.4f, 0.22f), dark);
        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0.2f, 0.3f, 0), new Vector3(0.22f, 0.4f, 0.22f), dark);

        var e = root.AddComponent<Enemy>();
        e.armL = Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(-0.5f, 1.35f, 0.15f), new Vector3(0.16f, 0.5f, 0.16f), skin).transform;
        e.armR = Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0.5f, 1.35f, 0.15f), new Vector3(0.16f, 0.5f, 0.16f), skin).transform;
        e.armL.localRotation = Quaternion.Euler(-60f, 0, 8f);
        e.armR.localRotation = Quaternion.Euler(-60f, 0, -8f);

        e.cc = root.GetComponent<CharacterController>();
        e.cc.height = 1.9f;
        e.cc.radius = 0.4f;
        e.cc.center = new Vector3(0, 0.95f, 0);
        e.cc.stepOffset = 0.4f;
        e.player = player;
        e.stats = stats;
        e.wanderDir = Vector3.forward;
        return e;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Update()
    {
        if (GameState.Frozen || player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        bool night = DayNightCycle.I != null && DayNightCycle.I.IsNight;
        float detect = night ? 55f : 30f;

        Vector3 move = Vector3.zero;
        Campfire fire = Campfire.Nearest(transform.position, 9f);

        if (fire != null)
        {
            Vector3 away = transform.position - fire.transform.position;
            away.y = 0f;
            move = away.normalized * chaseSpeed;
        }
        else if (dist < detect)
        {
            Vector3 dir = toPlayer / Mathf.Max(dist, 0.01f);
            if (dist > attackRange)
            {
                move = dir * chaseSpeed;
            }
            else
            {
                FaceDirection(dir, 12f);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    lungeAnim = 0.3f;
                    stats.Hurt(attackDamage);
                    HUD.Notify("Mutant te je udario!");
                }
            }
        }
        else
        {
            if (Time.time > wanderUntil)
            {
                wanderUntil = Time.time + Random.Range(3f, 7f);
                Vector2 r = Random.insideUnitCircle.normalized;
                wanderDir = Random.value < 0.3f ? Vector3.zero : new Vector3(r.x, 0, r.y);
            }
            move = wanderDir * walkSpeed;
        }

        if (move.sqrMagnitude > 0.01f) FaceDirection(move.normalized, 8f);

        knock = Vector3.Lerp(knock, Vector3.zero, 6f * Time.deltaTime);
        cc.SimpleMove(move + knock);

        // Animacija ruku
        if (lungeAnim > 0f) lungeAnim -= Time.deltaTime;
        float swing = lungeAnim > 0f ? -100f : -60f + Mathf.Sin(Time.time * 6f) * 10f;
        armL.localRotation = Quaternion.Euler(swing, 0, 8f);
        armR.localRotation = Quaternion.Euler(swing, 0, -8f);

        // Danju daleki mutanti nestaju
        if (!night && dist > 35f && Random.value < 0.02f) Destroy(gameObject);
    }

    void FaceDirection(Vector3 dir, float speed)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
    }

    public void Damage(float amount, Vector3 from)
    {
        hp -= amount;
        Vector3 back = transform.position - from;
        back.y = 0f;
        knock = back.normalized * 6f;
        if (hp <= 0f)
        {
            Pickup.Spawn(ItemType.RawMeat, transform.position + Vector3.forward * 0.3f);
            HUD.Notify("Pobedio si mutanta!");
            Destroy(gameObject);
        }
    }
}

// Zečevi: izvor hrane.
[RequireComponent(typeof(CharacterController))]
public class Animal : MonoBehaviour, IDamageable
{
    public static int Count;

    public float hp = 20f;
    CharacterController cc;
    Transform player;
    Vector3 wanderDir;
    float wanderUntil;

    void OnEnable() { Count++; }
    void OnDisable() { Count--; }

    public static Animal Spawn(Vector3 pos, Transform player)
    {
        pos.y = World.GroundY(pos.x, pos.z) + 0.1f;
        var root = new GameObject("Zec");
        root.transform.position = pos;

        Color fur = new Color(0.55f, 0.46f, 0.38f);
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.25f, 0), new Vector3(0.35f, 0.32f, 0.55f), fur);
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.38f, 0.28f), new Vector3(0.24f, 0.24f, 0.26f), fur);
        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(-0.06f, 0.6f, 0.26f), new Vector3(0.06f, 0.16f, 0.06f), fur);
        Prim.Make(PrimitiveType.Capsule, root.transform, new Vector3(0.06f, 0.6f, 0.26f), new Vector3(0.06f, 0.16f, 0.06f), fur);
        Prim.Make(PrimitiveType.Sphere, root.transform, new Vector3(0, 0.28f, -0.3f), Vector3.one * 0.12f, new Color(0.9f, 0.9f, 0.88f));

        var a = root.AddComponent<Animal>();
        a.cc = root.GetComponent<CharacterController>();
        a.cc.height = 0.6f;
        a.cc.radius = 0.25f;
        a.cc.center = new Vector3(0, 0.3f, 0);
        a.cc.stepOffset = 0.3f;
        a.player = player;
        return a;
    }

    void Awake() { cc = GetComponent<CharacterController>(); }

    void Update()
    {
        if (GameState.Frozen || player == null) return;

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        float dist = away.magnitude;
        Vector3 move;

        if (dist < 9f)
        {
            move = away.normalized * 6f;
        }
        else
        {
            if (Time.time > wanderUntil)
            {
                wanderUntil = Time.time + Random.Range(2f, 6f);
                Vector2 r = Random.insideUnitCircle.normalized;
                wanderDir = Random.value < 0.5f ? Vector3.zero : new Vector3(r.x, 0, r.y);
            }
            move = wanderDir * 1.2f;
        }

        if (move.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move.normalized), 10f * Time.deltaTime);

        // Ne silazi u vodu
        Vector3 next = transform.position + move * 0.3f;
        if (World.GroundY(next.x, next.z) < World.waterLevel + 0.3f) move = -move;

        cc.SimpleMove(move);
    }

    public void Damage(float amount, Vector3 from)
    {
        hp -= amount;
        if (hp <= 0f)
        {
            Pickup.SpawnAround(ItemType.RawMeat, transform.position, 2, 0.6f);
            Destroy(gameObject);
        }
    }
}
