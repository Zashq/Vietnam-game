using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FpsNewInput : MonoBehaviour
{
    [Header("Refs")]
    public Transform cam;

    [Header("Look")]
    public float lookSens = 0.12f;
    public float minPitch = -80f, maxPitch = 80f;

    [Header("Move & Jump")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 9f;
    public float jumpForce = 8f;        // bump this if heavy
    public float airControl = 0.4f;
    public LayerMask groundMask = ~0;   // set in Inspector to Default/Ground only

    // Add these fields near your other settings:
    [Header("Grounding")]
    public float groundCheckDistance = 0.2f;  // how far below feet to probe
    public float maxSlopeAngle = 55f;         // anything steeper = not grounded
    public bool debugGround = true;

    // Optional: inspect these at runtime
    Vector3 _groundNormal = Vector3.up;
    float _groundSlope;


    Rigidbody rb;
    CapsuleCollider col;
    float pitch;
    bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        col = GetComponent<CapsuleCollider>();
        if (!cam) cam = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // ha paused, semmit nem csinálunk
        if (Time.timeScale == 0f)
            return;

        // Look
        if (Mouse.current != null)
        {
            Vector2 d = Mouse.current.delta.ReadValue();
            transform.Rotate(0f, d.x * lookSens, 0f);
            pitch = Mathf.Clamp(pitch - d.y * lookSens, minPitch, maxPitch);
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // Queue jump
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpQueued = true;
    }

    void FixedUpdate()
    {
        if (Time.timeScale == 0f)
            return;

        if (Keyboard.current == null) return;

        // Movement
        float h = (Keyboard.current.aKey.isPressed ? -1f : 0f) + (Keyboard.current.dKey.isPressed ? 1f : 0f);
        float v = (Keyboard.current.sKey.isPressed ? -1f : 0f) + (Keyboard.current.wKey.isPressed ? 1f : 0f);
        Vector3 wish = (transform.forward * v + transform.right * h);
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        bool grounded = IsGrounded();
        float targetSpeed = Keyboard.current.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        Vector3 vel = rb.linearVelocity;
        Vector3 lateral = new Vector3(vel.x, 0f, vel.z);
        Vector3 desired = wish * targetSpeed;
        float accel = grounded ? 30f : 30f * airControl;
        Vector3 newLateral = Vector3.MoveTowards(lateral, desired, accel * Time.fixedDeltaTime);
        rb.linearVelocity = newLateral + Vector3.up * vel.y;

        // Jump — only when grounded
        if (grounded)
        {
            if (jumpQueued)
            {
                jumpQueued = false; // consume the input only on a valid jump
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                // Debug.Log("Jumped!");
            }
        }
        else
        {
            // if in air, ignore any jump presses
            jumpQueued = false;
        }


        // Extra gravity for tighter fall
        rb.AddForce(Physics.gravity * 0.6f, ForceMode.Acceleration);

        jumpQueued = false;
    }

    bool IsGrounded()
    {
        if (!col) col = GetComponent<CapsuleCollider>();

        // World-space capsule endpoints based on collider settings
        Vector3 center = transform.TransformPoint(col.center);
        float radius = Mathf.Max(0.01f, col.radius - 0.02f); // shrink a hair to avoid self-hits
        float half = Mathf.Max(0f, col.height * 0.5f - col.radius);

        Vector3 top = center + Vector3.up * half;
        Vector3 bottom = center - Vector3.up * half;

        // Start the cast just **above** the bottom to avoid starting inside ground
        Vector3 castTop = top;
        Vector3 castBottom = bottom + Vector3.up * 0.01f;

        bool hitGround = Physics.CapsuleCast(
            castTop, castBottom, radius,
            Vector3.down, out RaycastHit hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitGround)
        {
            _groundNormal = hit.normal;
            _groundSlope = Vector3.Angle(hit.normal, Vector3.up);

            if (debugGround)
            {
                Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, Color.cyan, 0, false);
                Debug.DrawLine(bottom, bottom + Vector3.down * groundCheckDistance, Color.green, 0, false);
            }

            // Steep surfaces are NOT “ground”
            return _groundSlope <= maxSlopeAngle;
        }
        else
        {
            if (debugGround)
                Debug.DrawLine(bottom, bottom + Vector3.down * groundCheckDistance, Color.red, 0, false);

            _groundNormal = Vector3.up;
            _groundSlope = 0f;
            return false;
        }
    }

}
