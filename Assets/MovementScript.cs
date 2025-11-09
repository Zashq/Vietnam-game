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

        // Jump
        if (grounded && jumpQueued)
        {
            jumpQueued = false;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            // Debug
            // Debug.Log("JUMP! grounded=true, applied v.y=" + jumpForce);
        }

        // Extra gravity for tighter fall
        rb.AddForce(Physics.gravity * 0.6f, ForceMode.Acceleration);

        jumpQueued = false;
    }

    bool IsGrounded()
    {
        // Check a capsule just slightly below feet against ONLY groundMask
        float skin = 0.05f;
        float r = Mathf.Max(0.01f, col.radius * 0.95f);
        Vector3 p1 = transform.position + Vector3.up * (col.height * 0.5f - r);
        Vector3 p2 = transform.position + Vector3.up * (r);
        bool hit = Physics.CheckCapsule(p1, p2, r - 0.01f, groundMask, QueryTriggerInteraction.Ignore);

        // Debug sanity
        // if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        //     Debug.Log($"Grounded={hit}, rb.constraints={rb.constraints}");

        return hit;
    }
}
