using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float health = 100f;
    public float hunger = 100f;
    public float thirst = 100f;
    public float stamina = 100f;
    public float hurtFlash;

    void Update()
    {
        if (GameState.Frozen) return;
        float dt = Time.deltaTime;

        hunger = Mathf.Max(0f, hunger - 0.14f * dt);
        thirst = Mathf.Max(0f, thirst - 0.20f * dt);

        if (hunger <= 0f || thirst <= 0f)
            health -= 1.2f * dt;
        else if (hunger > 50f && thirst > 50f)
            health = Mathf.Min(100f, health + 0.6f * dt);

        if (hurtFlash > 0f) hurtFlash -= dt;

        if (health <= 0f && !GameState.Dead)
        {
            health = 0f;
            GameState.Dead = true;
        }
    }

    public void Hurt(float amount)
    {
        if (GameState.Frozen) return;
        health -= amount;
        hurtFlash = 0.5f;
    }

    public void Eat(float v) { hunger = Mathf.Min(100f, hunger + v); }
    public void Drink(float v) { thirst = Mathf.Min(100f, thirst + v); }
}
