using System.Collections;
using UnityEngine;


public class WPN_AKM : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float fireRate = 600f; // rounds per minute
    public int magazineSize = 30;
    public int currentAmmo;

    [Header("Reload")]
    public float reloadTime = 2.0f;       // reload delay
    private bool isReloading = false;

    [Header("References")]
    public Transform muzzlePoint;
    public LayerMask hitMask;
    public GameObject bulletPrefab;

    private float nextShootTime = 0f;

    void Start()
    {
        currentAmmo = magazineSize;
    }

    void Update()
    {
        // ha épp reloadol → nem lőhetsz
        if (isReloading) return;

        // R → reload
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentAmmo < magazineSize)
                StartCoroutine(Reload());
        }

        // lövés
        if (Input.GetMouseButton(0))
        {
            TryShoot();
        }
    }

    IEnumerator Reload()
    {
        isReloading = true;
        Debug.Log("Reloading...");

        yield return new WaitForSeconds(reloadTime);

        currentAmmo = magazineSize;
        Debug.Log("Reload complete. Ammo refilled.");

        isReloading = false;
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

        // Vizualis bullet
        if (bulletPrefab != null && muzzlePoint != null)
        {
            Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        }

        // Raycast találat
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out RaycastHit hit, 200f, hitMask))
        {
            Debug.Log("Hit " + hit.collider.name);

            Health hp = hit.collider.GetComponent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(30f);
            }
        }
    }
}
