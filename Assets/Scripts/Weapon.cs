using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Weapon : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;
    private WeaponPickup pickup;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        pickup = GetComponent<WeaponPickup>();
    }

    // This is what WeaponManager is trying to call
    public void OnEquip(Transform hand)
    {
        Debug.Log("OnEquip " + name);

        transform.SetParent(hand);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        col.enabled = false;
        if (pickup) pickup.enabled = false;
    }

    // This is what WeaponManager is trying to call on drop
    public void OnDrop()
    {
        Debug.Log("OnDrop " + name);

        transform.SetParent(null);

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        col.enabled = true;
        if (pickup) pickup.enabled = true;

        // optional: small forward push
        if (Camera.main != null)
        {
            Vector3 force = Camera.main.transform.forward * 3f + Vector3.up * 1f;
            rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
