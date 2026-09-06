using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkObject), typeof(NetworkTransform), typeof(Rigidbody))]
public class MultiplayerPlayer : NetworkBehaviour
{
    public Camera playerCamera;
    public Transform muzzle;
    public Renderer bodyRenderer;
    public GameObject firstPersonWeapons;
    public Material redMaterial;
    public Material blueMaterial;
    public float walkSpeed = 6f, sprintSpeed = 9f, jumpForce = 5.5f, sensitivity = 0.12f;
    public float roundsPerMinute = 600f;
    public int magazineSize = 30;
    public float reloadSeconds = 2f;
    public readonly NetworkVariable<int> Team = new NetworkVariable<int>();
    public readonly NetworkVariable<int> HitPoints = new NetworkVariable<int>();
    public readonly NetworkVariable<int> Ammo = new NetworkVariable<int>();
    public readonly NetworkVariable<bool> Reloading = new NetworkVariable<bool>();
    public readonly NetworkVariable<int> Kills = new NetworkVariable<int>();
    public readonly NetworkVariable<int> Deaths = new NetworkVariable<int>();
    private Rigidbody body;
    private CapsuleCollider capsule;
    private NetworkTransform networkTransform;
    private float pitch, nextLocalShot;
    private bool jumpQueued;
    private double nextServerShot, reloadAt, respawnAt, protectedUntil;
    private float lastServerY, fallPeak;
    private bool falling;
    private bool CanControl => IsSpawned && IsOwner && HitPoints.Value > 0 &&
        !MultiplayerMenu.InputBlocked && TeamDeathmatch.Instance && TeamDeathmatch.Instance.Winner.Value < 0;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        networkTransform = GetComponent<NetworkTransform>();
        body.isKinematic = true;
        SetLocalView(false);
    }

    public override void OnNetworkSpawn()
    {
        body.isKinematic = !IsOwner;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        Team.OnValueChanged += OnTeamChanged;
        HitPoints.OnValueChanged += OnHealthChanged;
        ApplyAppearance();
        SetLocalView(IsOwner);
        if (TeamDeathmatch.Instance && TeamDeathmatch.Instance.IsSpawned) TeamDeathmatch.Instance.Register(this);
    }

    private void SetLocalView(bool local)
    {
        if (playerCamera)
        {
            playerCamera.enabled = local;
            var listener = playerCamera.GetComponent<AudioListener>();
            if (listener) listener.enabled = local;
        }
        if (firstPersonWeapons) firstPersonWeapons.SetActive(local);
        if (bodyRenderer) bodyRenderer.enabled = !local;
    }

    private void OnTeamChanged(int before, int after) { ApplyAppearance(); }
    private void OnHealthChanged(int before, int after)
    {
        if (bodyRenderer) bodyRenderer.enabled = !IsOwner && after > 0;
        if (IsOwner && body && !body.isKinematic && after <= 0) body.linearVelocity = Vector3.zero;
    }
    private void ApplyAppearance()
    {
        if (bodyRenderer) bodyRenderer.sharedMaterial = Team.Value == 0 ? redMaterial : blueMaterial;
    }

    private void Update()
    {
        if (!IsSpawned) return;
        if (IsServer) ServerTick();
        if (!CanControl)
        {
            jumpQueued = false;
            if (IsOwner && playerCamera) playerCamera.fieldOfView = 60;
            return;
        }
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        if (mouse != null)
        {
            var delta = mouse.delta.ReadValue();
            transform.Rotate(0, delta.x * sensitivity, 0);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -80, 80);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            playerCamera.fieldOfView = mouse.rightButton.isPressed ? 40 : 60;
            if (mouse.leftButton.isPressed && Time.unscaledTime >= nextLocalShot && Ammo.Value > 0 && !Reloading.Value)
            {
                nextLocalShot = Time.unscaledTime + 60f / roundsPerMinute;
                FireRpc(playerCamera.transform.forward);
            }
        }
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame) jumpQueued = true;
            if (keyboard.rKey.wasPressedThisFrame) ReloadRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || !IsOwner || body.isKinematic) return;
        var keyboard = Keyboard.current;
        Vector3 wish = Vector3.zero;
        bool sprint = false;
        if (CanControl && keyboard != null)
        {
            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            wish = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1);
            sprint = keyboard.leftShiftKey.isPressed;
        }
        bool grounded = IsGrounded();
        Vector3 velocity = body.linearVelocity;
        Vector3 lateral = Vector3.MoveTowards(new Vector3(velocity.x, 0, velocity.z),
            wish * (sprint ? sprintSpeed : walkSpeed), (grounded ? 30 : 12) * Time.fixedDeltaTime);
        body.linearVelocity = lateral + Vector3.up * velocity.y;
        if (CanControl && grounded && jumpQueued)
        {
            body.linearVelocity = lateral;
            body.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        jumpQueued = false;
        body.AddForce(Physics.gravity * 0.6f, ForceMode.Acceleration);
    }

    private bool IsGrounded()
    {
        var center = transform.TransformPoint(capsule.center);
        float half = capsule.height * 0.5f - capsule.radius;
        foreach (var hit in Physics.CapsuleCastAll(center + Vector3.up * half,
                     center - Vector3.up * half + Vector3.up * 0.02f, capsule.radius * 0.9f,
                     Vector3.down, 0.22f, ~0, QueryTriggerInteraction.Ignore))
            if (hit.collider.transform.root != transform && Vector3.Angle(hit.normal, Vector3.up) <= 55) return true;
        return false;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void FireRpc(Vector3 direction)
    {
        var match = TeamDeathmatch.Instance;
        double now = NetworkManager.ServerTime.Time;
        if (!match || match.Winner.Value >= 0 || HitPoints.Value <= 0 || Reloading.Value || Ammo.Value <= 0 || now < nextServerShot) return;
        if (!IsFinite(direction) || direction.sqrMagnitude < 0.9f || direction.sqrMagnitude > 1.1f) return;
        nextServerShot = now + 60f / roundsPerMinute;
        Ammo.Value--;
        // Never accept an origin, target, damage value, or ammo count supplied by the client.
        Vector3 origin = playerCamera.transform.position;
        var hits = Physics.RaycastAll(origin, direction.normalized, 500, ~0, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        Vector3 end = origin + direction.normalized * 500;
        bool didHit = false;
        foreach (var hit in hits)
        {
            var target = hit.collider.GetComponentInParent<MultiplayerPlayer>();
            if (target == this || (target && target.HitPoints.Value <= 0)) continue;
            end = hit.point;
            didHit = true;
            if (target && target.Team.Value != Team.Value) target.ServerDamage(30, this);
            break; // World geometry and teammates both block the shot.
        }
        ShotFxRpc(origin, end, didHit);
    }

    private static bool IsFinite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) &&
        !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void ReloadRpc()
    {
        if (HitPoints.Value <= 0 || Reloading.Value || Ammo.Value >= magazineSize || !TeamDeathmatch.Instance || TeamDeathmatch.Instance.Winner.Value >= 0) return;
        Reloading.Value = true;
        reloadAt = NetworkManager.ServerTime.Time + reloadSeconds;
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
    private void ShotFxRpc(Vector3 origin, Vector3 end, bool hit)
    {
        var trail = new GameObject("Shot tracer");
        var line = trail.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.025f;
        line.endWidth = 0.01f;
        line.sharedMaterial = Team.Value == 0 ? redMaterial : blueMaterial;
        line.SetPosition(0, IsOwner && muzzle ? muzzle.position : origin);
        line.SetPosition(1, end);
        Destroy(trail, 0.06f);
        if (hit)
        {
            var impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "Shot impact";
            impact.transform.position = end;
            impact.transform.localScale = Vector3.one * 0.07f;
            impact.GetComponent<Collider>().enabled = false;
            Destroy(impact.GetComponent<Collider>());
            Destroy(impact, 0.15f);
        }
    }

    public void ServerDamage(int amount, MultiplayerPlayer attacker)
    {
        if (!IsServer || HitPoints.Value <= 0 || amount <= 0 || !TeamDeathmatch.Instance ||
            TeamDeathmatch.Instance.Winner.Value >= 0 || NetworkManager.ServerTime.Time < protectedUntil) return;
        HitPoints.Value = Mathf.Max(0, HitPoints.Value - amount);
        if (HitPoints.Value != 0) return;
        Reloading.Value = false;
        respawnAt = NetworkManager.ServerTime.Time + TeamDeathmatch.Instance.respawnDelay;
        TeamDeathmatch.Instance.RecordKill(this, attacker);
    }

    private void ServerTick()
    {
        if (!TeamDeathmatch.Instance || TeamDeathmatch.Instance.Winner.Value >= 0) return;
        double now = NetworkManager.ServerTime.Time;
        if (HitPoints.Value <= 0)
        {
            if (respawnAt > 0 && now >= respawnAt) ServerRespawn();
            return;
        }
        if (Reloading.Value && now >= reloadAt) { Ammo.Value = magazineSize; Reloading.Value = false; }
        float y = transform.position.y;
        if (y < -100) ServerDamage(100, null);
        // Position deltas also work for the kinematic server copies of remote players.
        if (y < lastServerY - 0.01f && !falling) { falling = true; fallPeak = lastServerY; }
        if (falling && IsGrounded())
        {
            float distance = fallPeak - y;
            falling = false;
            if (distance > 4) ServerDamage(distance >= 15 ? 100 : Mathf.CeilToInt((distance - 4) * 5), null);
        }
        lastServerY = y;
    }

    public void ServerRespawn()
    {
        if (!IsServer || !TeamDeathmatch.Instance) return;
        var spawn = TeamDeathmatch.Instance.GetSpawn(Team.Value);
        HitPoints.Value = 100;
        Ammo.Value = magazineSize;
        Reloading.Value = false;
        respawnAt = 0;
        nextServerShot = 0;
        protectedUntil = NetworkManager.ServerTime.Time + 1;
        falling = false;
        lastServerY = spawn.position.y;
        transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        RespawnRpc(spawn.position, spawn.rotation);
    }

    [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
    private void RespawnRpc(Vector3 position, Quaternion rotation)
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(position, rotation);
        networkTransform.Teleport(position, rotation, transform.localScale);
        pitch = 0;
        playerCamera.transform.localRotation = Quaternion.identity;
        nextLocalShot = 0;
        jumpQueued = false;
    }

    public override void OnNetworkDespawn()
    {
        Team.OnValueChanged -= OnTeamChanged;
        HitPoints.OnValueChanged -= OnHealthChanged;
        if (TeamDeathmatch.Instance) TeamDeathmatch.Instance.Players.Remove(this);
        SetLocalView(false);
    }
}
