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

    // FPS
    private float fps;
    private float deltaTime;

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

        // FPS calculation
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fps = 1f / deltaTime;

        // Player speeds
        Vector3 vel = playerRb.linearVelocity;
        speed = new Vector3(vel.x, 0f, vel.z).magnitude;
        verticalSpeed = vel.y;

        // Ground probe
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

        GUI.Label(new Rect(20, y, 250, 25), $"FPS: {fps:F0}", style); y += 25;
        GUI.Label(new Rect(20, y, 250, 25), $"Speed: {speed:F2} m/s", style); y += 25;

        if (showVerticalSpeed)
            GUI.Label(new Rect(20, y, 250, 25), $"Vertical: {verticalSpeed:F2} m/s", style); y += 25;

        GUI.Label(new Rect(20, y, 250, 25), $"Grounded: {grounded}", style); y += 25;
        GUI.Label(new Rect(20, y, 250, 25), $"Slope: {slopeAngle:F1}°", style); y += 25;
        GUI.Label(new Rect(20, y, 450, 25), $"Position: {playerTransform.position}", style); y += 25;
    }
}
