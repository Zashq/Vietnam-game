using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons (order matters)")]
    public GameObject[] weapons;

    private int currentIndex = 0;

    void Start()
    {
        if (weapons == null || weapons.Length == 0)
        {
            Debug.LogError("WeaponManager: nincsenek beállítva fegyverek.");
            return;
        }

        SelectWeapon(currentIndex);
    }

    void Update()
    {
        if (weapons == null || weapons.Length == 0) return;

        // 1-es fegyver
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (weapons.Length >= 1)
                SelectWeapon(0);
        }

        // 2-es fegyver
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (weapons.Length >= 2)
                SelectWeapon(1);
        }

        // Egérgörgő – scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            NextWeapon();
        }
        else if (scroll < 0f)
        {
            PreviousWeapon();
        }
    }

    void NextWeapon()
    {
        int newIndex = currentIndex + 1;
        if (newIndex >= weapons.Length)
            newIndex = 0;

        SelectWeapon(newIndex);
    }

    void PreviousWeapon()
    {
        int newIndex = currentIndex - 1;
        if (newIndex < 0)
            newIndex = weapons.Length - 1;

        SelectWeapon(newIndex);
    }

    void SelectWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length)
        {
            Debug.LogWarning("WeaponManager: invalid weapon index " + index);
            return;
        }

        currentIndex = index;

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].SetActive(i == currentIndex);
        }

        Debug.Log("WeaponManager: aktív fegyver = " + weapons[currentIndex].name);
    }
}
