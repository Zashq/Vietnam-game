using UnityEngine;
using System.Collections;


public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Fall Damage Settings")]
    public float minFallToDamage = 4f;     // ennél kisebb esés semmit nem sebez
    public float deathFallHeight = 15f;    // ha ekkora esést meghalad → instant halál
    public float damageMultiplier = 5f;    // fallDistance * multiplier = sebzés

    private bool isDead = false;

    // fall tracking
    private bool isFalling = false;
    private float fallStartY;
    private Rigidbody rb;

    void Start()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        HandleFallDetection();
    }

    private void HandleFallDetection()
    {
        // falling start
        if (!isFalling && rb.linearVelocity.y < -0.5f)
        {
            isFalling = true;
            fallStartY = transform.position.y;
        }

        // landing detected
        if (isFalling && rb.linearVelocity.y > -0.1f)
        {
            isFalling = false;
            float fallDistance = fallStartY - transform.position.y;

            if (fallDistance > minFallToDamage)
            {
                ApplyFallDamage(fallDistance);
            }
        }
    }

    private void ApplyFallDamage(float fallDistance)
    {
        // instant kill
        if (fallDistance >= deathFallHeight)
        {
            Debug.Log($"Fall Death: esés={fallDistance}m");
            Die();
            return;
        }

        // fall damage = magasság * szorzó
        float dmg = (fallDistance - minFallToDamage) * damageMultiplier;

        Debug.Log($"Fall Damage: {dmg} (distance {fallDistance}m)");
        TakeDamage(dmg);
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
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} died!");

        StartCoroutine(Respawn());
    }

    public IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f); // optional delay

        // reset state
        currentHP = maxHP;
        isDead = false;

        // reset velocity
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // teleport to respawn point
        transform.SetPositionAndRotation(
            RespawnPoint.instance.transform.position,
            RespawnPoint.instance.transform.rotation
        );

        Debug.Log("Respawned.");
    }

    public void ForceRespawn()
    {
        StopAllCoroutines(); // biztos ami biztos

        currentHP = maxHP;
        isDead = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.SetPositionAndRotation(
            RespawnPoint.instance.transform.position,
            RespawnPoint.instance.transform.rotation
        );

        Debug.Log("Respawned via Pause Menu.");
    }



}
