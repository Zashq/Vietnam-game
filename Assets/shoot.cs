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
    public Camera playerCam;
    public Transform muzzlePoint;
    public LayerMask hitMask;
    public GameObject bulletPrefab;
    public GameObject impactPrefab;

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

        // CAMERA → CENTER RAY
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, hitMask))
        {
            targetPoint = hit.point;

            // IMPACT MARKER A TALÁLATI PONTNÁL
            if (impactPrefab != null)
            {
                // orientáljuk a felület normálja szerint
                Quaternion rot = Quaternion.LookRotation(hit.normal * -1f);
                GameObject marker = Instantiate(impactPrefab, hit.point, rot);
                Destroy(marker, 2f); // 2 mp múlva eltűnik
            }

            // DAMAGE
            Health hp = hit.collider.GetComponent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(30f);
            }
        }
        else
        {
            targetPoint = ray.origin + ray.direction * 500f;
        }

        // VIZUÁLIS GOLYÓ A CSŐBŐL CÉLPONT FELÉ
        if (bulletPrefab != null && muzzlePoint != null)
        {
            Vector3 dir = (targetPoint - muzzlePoint.position).normalized;
            Instantiate(bulletPrefab, muzzlePoint.position, Quaternion.LookRotation(dir));
        }
    }


}
