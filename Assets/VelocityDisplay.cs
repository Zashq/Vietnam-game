using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    public Rigidbody playerRb;
    public Transform playerTransform;
    public bool showVerticalSpeed = true;

    private GUIStyle style;
    private float speed;
    private float verticalSpeed;
    private bool grounded;
    private float slopeAngle;

    void Start()
    {
        if (!playerRb) playerRb = GetComponentInParent<Rigidbody>();
        if (!playerTransform) playerTransform = playerRb.transform;

        style = new GUIStyle
        {
            fontSize = 25,
            normal = new GUIStyleState { textColor = Color.white }
        };
    }

    void Update()
    {
        if (!playerRb) return;

        Vector3 vel = playerRb.linearVelocity;
        speed = new Vector3(vel.x, 0f, vel.z).magnitude;
        verticalSpeed = vel.y;

        // Simple ground probe
        if (Physics.Raycast(playerTransform.position, Vector3.down, out RaycastHit hit, 1.2f))
        {
            grounded = true;
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
        }
        else
        {
            grounded = false;
            slopeAngle = 0f;
        }
    }

    void OnGUI()
    {
        float y = 20;
        GUI.Label(new Rect(20, y, 250, 25), $"Speed: {speed:F2} m/s", style); y += 20;
        if (showVerticalSpeed)
            GUI.Label(new Rect(20, y, 250, 25), $"Vertical: {verticalSpeed:F2} m/s", style); y += 20;
        GUI.Label(new Rect(20, y, 250, 25), $"Grounded: {grounded}", style); y += 20;
        GUI.Label(new Rect(20, y, 250, 25), $"Slope: {slopeAngle:F1}�", style); y += 20;
        GUI.Label(new Rect(20, y, 250, 25), $"Position: {playerTransform.position}", style); y += 20;
    }
}
