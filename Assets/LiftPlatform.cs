using UnityEngine;

public class LiftPlatform : MonoBehaviour
{
    [Header("Lift Settings")]
    public float targetHeight = 5f;
    public float liftSpeed = 2f;
    public float decreaseSpeed = -2f;

    private bool lifting = false;
    private bool decrease = false;
    private float startHeight;

    void Start()
    {
        startHeight = transform.position.y;
    }

    void Update()
    {
        // ha lenyomod az U-t → indul a lift
        if (Input.GetKeyDown(KeyCode.U))
        {
            lifting = true;
            decrease = false ;
        }

        if (lifting && !decrease)
        {
            float currentHeight = transform.position.y;

            if (currentHeight < startHeight + targetHeight)
            {
                transform.position += Vector3.up * liftSpeed * Time.deltaTime;
            }
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            lifting = false ;
            decrease = true;
        }

        if (decrease && !lifting)
        {
            float currentHeight = transform.position.y;

            if (currentHeight > startHeight)
            {
                transform.position -= Vector3.up * decreaseSpeed * Time.deltaTime;
            }
        }
    }
}
