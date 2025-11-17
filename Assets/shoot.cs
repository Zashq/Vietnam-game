using UnityEngine;

public class WPN_AKM : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 600f; // rounds per minute
    public int magazineSize = 30;
    public int currentAmmo;

    [Header("References")]
    public Transform muzzlePoint;
    public LayerMask hitMask;

    private float nextShootTime = 0f;

    void Start()
    {
        currentAmmo = magazineSize;
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) // LEFT MOUSE HOLD
        {
            TryShoot();
        }
    }

    void TryShoot()
    {
        if (Time.time < nextShootTime) return;
        if (currentAmmo <= 0)
        {
            Debug.Log("CLICK – no ammo");
            return;
        }

        nextShootTime = Time.time + 60f / fireRate;
        Shoot();
    }

    void Shoot()
    {
        currentAmmo--;

        // Raycast
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out RaycastHit hit, 200f, hitMask))
        {
            Debug.Log("Hit " + hit.collider.name);

            // ha van health
            hit.collider.GetComponent<Health>()?.TakeDamage(30f);
        }
    }
}
