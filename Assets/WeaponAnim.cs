using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    public Animator anim;
    public string aimBoolName = "IsAiming";
    public GameObject crosshair;

    private bool isAiming = false;

    void Update()
    {
        bool rmbHeld = Input.GetMouseButton(1);

        if (rmbHeld && !isAiming)
        {
            isAiming = true;
            anim.SetBool(aimBoolName, true);
            Camera.main.fieldOfView = 40f; // aim

            // crosshair hide
            if (crosshair != null)
                crosshair.SetActive(false);
        }
        else if (!rmbHeld && isAiming)
        {
            isAiming = false;
            anim.SetBool(aimBoolName, false);
            Camera.main.fieldOfView = 60f; // normal

            // crosshair show
            if (crosshair != null)
                crosshair.SetActive(true);
        }
    }
}
