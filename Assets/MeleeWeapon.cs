using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [Header("Melee Settings")]
    public float damage = 25f;
    public float range = 2f;
    public float attackCooldown = 0.6f;

    private float nextAttackTime = 0f;

    [Header("References")]
    public Camera playerCam;
    public Animator anim;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryAttack();
        }
    }

    void TryAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        // PLAY ATTACK ANIMATION
        anim.SetTrigger("AttackTrigger");
    }

    // THIS WILL BE CALLED FROM AN ANIMATION EVENT
    public void DoDamage()
    {
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            Health target = hit.collider.GetComponent<Health>();
            if (target != null)
            {
                target.TakeDamage(damage);
                Debug.Log("BOT melee hit: " + hit.collider.name);
            }
        }
    }
}
