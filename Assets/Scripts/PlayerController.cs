using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Camera cam;
    public Inventory inv;
    public PlayerStats stats;
    public string Hint = "";
    public bool InWater;

    CharacterController cc;
    float yaw, pitch;
    float vy;
    float nextHit;
    float swing;
    Transform axe;

    const float WalkSpeed = 4.5f;
    const float SprintSpeed = 7.8f;
    const float JumpSpeed = 6.0f;
    const float Gravity = -20f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.45f;
        inv = gameObject.AddComponent<Inventory>();
        stats = gameObject.AddComponent<PlayerStats>();
    }

    public void BuildCamera()
    {
        var camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(transform, false);
        camGo.transform.localPosition = new Vector3(0, 1.65f, 0);
        cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 72f;
        cam.nearClipPlane = 0.08f;
        cam.farClipPlane = 700f;
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.AddComponent<AudioListener>();

        // Sekira u ruci
        var holder = new GameObject("AxeHolder");
        holder.transform.SetParent(camGo.transform, false);
        holder.transform.localPosition = new Vector3(0.38f, -0.32f, 0.65f);
        axe = holder.transform;
        Prim.Make(PrimitiveType.Cube, axe, new Vector3(0, 0, 0), new Vector3(0.05f, 0.55f, 0.05f), new Color(0.45f, 0.3f, 0.15f));
        Prim.Make(PrimitiveType.Cube, axe, new Vector3(0.08f, 0.24f, 0), new Vector3(0.2f, 0.13f, 0.04f), new Color(0.55f, 0.56f, 0.6f), false, 0.5f);
        axe.localRotation = Quaternion.Euler(10f, -20f, -12f);
    }

    public void SetYaw(float degrees)
    {
        yaw = degrees;
        transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    void Update()
    {
        if (GameState.Frozen) return;

        Look();
        Move();
        Scan();
        Actions();
        AnimateAxe();

        if (transform.position.y < -20f) transform.position = World.spawn;
    }

    void Look()
    {
        yaw += Input.GetAxis("Mouse X") * 2.2f;
        pitch -= Input.GetAxis("Mouse Y") * 2.2f;
        pitch = Mathf.Clamp(pitch, -85f, 85f);
        transform.rotation = Quaternion.Euler(0, yaw, 0);
        cam.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }

    void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 dir = (transform.right * x + transform.forward * z);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        InWater = transform.position.y < World.waterLevel - 0.2f;

        bool wantSprint = Input.GetKey(KeyCode.LeftShift) && z > 0f && stats.stamina > 1f && !InWater;
        float speed = wantSprint ? SprintSpeed : WalkSpeed;
        if (InWater) speed *= 0.55f;

        if (wantSprint && dir.sqrMagnitude > 0.1f) stats.stamina = Mathf.Max(0f, stats.stamina - 16f * Time.deltaTime);
        else stats.stamina = Mathf.Min(100f, stats.stamina + 12f * Time.deltaTime);

        if (cc.isGrounded)
        {
            vy = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && stats.stamina > 8f)
            {
                vy = JumpSpeed;
                stats.stamina -= 8f;
            }
        }
        else vy += Gravity * Time.deltaTime;

        Vector3 vel = dir * speed + Vector3.up * vy;
        cc.Move(vel * Time.deltaTime);

        // Granica sveta
        Vector3 c = new Vector3(World.size * 0.5f, transform.position.y, World.size * 0.5f);
        Vector3 off = transform.position - c;
        off.y = 0f;
        float maxR = World.size * 0.46f;
        if (off.magnitude > maxR)
        {
            Vector3 clamped = c + off.normalized * maxR;
            cc.enabled = false;
            transform.position = new Vector3(clamped.x, transform.position.y, clamped.z);
            cc.enabled = true;
            HUD.Notify("Okean je previše opasan. Vrati se na ostrvo.");
        }
    }

    void Scan()
    {
        Hint = "";
        var ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 3.2f, ~0, QueryTriggerInteraction.Collide))
        {
            var it = hit.collider.GetComponentInParent<IInteractable>();
            if (it != null) { Hint = it.GetHint(this); return; }
            if (hit.collider.GetComponentInParent<TreeObj>() != null) { Hint = "Levi klik: seci drvo"; return; }
        }
        if (InWater) Hint = "E: pij vodu";
    }

    void Actions()
    {
        // Napad / sečenje
        if (Input.GetMouseButtonDown(0) && Time.time >= nextHit)
        {
            nextHit = Time.time + 0.55f;
            swing = 1f;
            float dmg = inv.stoneAxe ? 40f : 25f;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 3.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                var d = hit.collider.GetComponentInParent<IDamageable>();
                if (d != null) d.Damage(dmg, transform.position);
            }
        }

        // Interakcija
        if (Input.GetKeyDown(KeyCode.E))
        {
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;
            IInteractable it = null;
            if (Physics.Raycast(ray, out hit, 3.2f, ~0, QueryTriggerInteraction.Collide))
                it = hit.collider.GetComponentInParent<IInteractable>();

            if (it != null) it.Interact(this);
            else if (InWater)
            {
                stats.Drink(25f);
                HUD.Notify("Popio si vode");
            }
        }

        // Jelo
        if (Input.GetKeyDown(KeyCode.F)) Eat();

        // Pečenje mesa
        if (Input.GetKeyDown(KeyCode.C))
        {
            var fire = Campfire.Nearest(transform.position, 3.5f);
            if (fire == null) HUD.Notify("Treba ti upaljena vatra u blizini.");
            else if (inv.Remove(ItemType.RawMeat, 1)) { inv.Add(ItemType.CookedMeat); HUD.Notify("Ispekao si meso"); }
            else HUD.Notify("Nemaš sirovog mesa.");
        }

        // Pravljenje
        if (Input.GetKeyDown(KeyCode.Alpha1)) CraftCampfire();
        if (Input.GetKeyDown(KeyCode.Alpha2)) CraftShelter();
        if (Input.GetKeyDown(KeyCode.Alpha3)) CraftStoneAxe();
    }

    void Eat()
    {
        if (inv.Remove(ItemType.CookedMeat, 1)) { stats.Eat(40f); HUD.Notify("Pojeo si pečeno meso (+40)"); }
        else if (inv.Remove(ItemType.Berry, 1)) { stats.Eat(8f); stats.Drink(4f); HUD.Notify("Pojeo si bobice (+8)"); }
        else if (inv.Remove(ItemType.RawMeat, 1)) { stats.Eat(18f); stats.Hurt(6f); HUD.Notify("Sirovo meso te je razbolelo (-6 zdravlja)"); }
        else HUD.Notify("Nemaš šta da pojedeš.");
    }

    Vector3 PlacePoint()
    {
        Vector3 f = transform.forward;
        f.y = 0f;
        f.Normalize();
        return transform.position + f * 2.6f;
    }

    void CraftCampfire()
    {
        if (!inv.Has(ItemType.Wood, 2) || !inv.Has(ItemType.Stick, 3))
        {
            HUD.Notify("Vatra: treba 2 drva + 3 štapa");
            return;
        }
        Vector3 p = PlacePoint();
        if (World.GroundY(p.x, p.z) < World.waterLevel + 0.3f) { HUD.Notify("Ne možeš ovde, previše vode."); return; }
        inv.Remove(ItemType.Wood, 2);
        inv.Remove(ItemType.Stick, 3);
        Campfire.Create(p);
        HUD.Notify("Napravio si logorsku vatru");
    }

    void CraftShelter()
    {
        if (!inv.Has(ItemType.Wood, 6) || !inv.Has(ItemType.Stick, 4))
        {
            HUD.Notify("Sklonište: treba 6 drva + 4 štapa");
            return;
        }
        Vector3 p = PlacePoint() + transform.forward * 1.2f;
        if (World.GroundY(p.x, p.z) < World.waterLevel + 0.3f) { HUD.Notify("Ne možeš ovde, previše vode."); return; }
        inv.Remove(ItemType.Wood, 6);
        inv.Remove(ItemType.Stick, 4);
        Shelter.Create(p, transform.eulerAngles.y + 180f);
        HUD.Notify("Napravio si sklonište (E u njemu = spavanje noću)");
    }

    void CraftStoneAxe()
    {
        if (inv.stoneAxe) { HUD.Notify("Već imaš kamenu sekiru."); return; }
        if (!inv.Has(ItemType.Stone, 3) || !inv.Has(ItemType.Stick, 2))
        {
            HUD.Notify("Kamena sekira: treba 3 kamena + 2 štapa");
            return;
        }
        inv.Remove(ItemType.Stone, 3);
        inv.Remove(ItemType.Stick, 2);
        inv.stoneAxe = true;
        HUD.Notify("Napravio si kamenu sekiru (jači udarci)");
    }

    void AnimateAxe()
    {
        if (axe == null) return;
        if (swing > 0f) swing = Mathf.Max(0f, swing - Time.deltaTime * 3.2f);
        float t = Mathf.Sin(swing * Mathf.PI);
        axe.localRotation = Quaternion.Euler(10f + t * 70f, -20f + t * 15f, -12f);
        axe.localPosition = new Vector3(0.38f, -0.32f - t * 0.1f, 0.65f + t * 0.1f);
    }
}
