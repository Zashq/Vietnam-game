using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    public float currentHP;

    private bool isDead = false;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        Debug.Log($"{gameObject.name} took {dmg} damage. HP: {currentHP}");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} died!");

        // ide rakhatsz bármit:
        // - animáció
        // - ragdoll
        // - delayed destroy
        // - particle effect

        Destroy(gameObject);
    }
}
