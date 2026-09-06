using UnityEngine;
using UnityEngine.UI;

public class HUD_UI : MonoBehaviour
{
    [Header("Refs")]
    public WPN_AKM weapon;        // ide húzod a fegyvert
    public Health playerHealth;   // ide a player Health-jét

    [Header("UI Elements")]
    public Text ammoText;
    public Text healthText;

    void Update()
    {
        if (weapon != null && ammoText != null)
        {
            ammoText.text = $"Ammo: {weapon.currentAmmo} / {weapon.magazineSize}";
        }

        if (playerHealth != null && healthText != null)
        {
            // egészre kerekítve
            healthText.text = $"HP: {Mathf.CeilToInt(playerHealth.currentHP)}";
        }
    }
}
