using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>Server-owned match rules. Team 0 is Red; team 1 is Blue.</summary>
public class TeamDeathmatch : NetworkBehaviour
{
    public static TeamDeathmatch Instance { get; private set; }
    public int scoreLimit = 25;
    public float respawnDelay = 3f;
    public Transform[] redSpawns;
    public Transform[] blueSpawns;
    public readonly NetworkVariable<int> RedScore = new NetworkVariable<int>();
    public readonly NetworkVariable<int> BlueScore = new NetworkVariable<int>();
    public readonly NetworkVariable<int> Winner = new NetworkVariable<int>(-1);
    public readonly List<MultiplayerPlayer> Players = new List<MultiplayerPlayer>();
    private double restartAt;
    private int spawnSequence;

    private void Awake() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        // Scene objects and player objects can spawn in either order.
        foreach (var player in FindObjectsByType<MultiplayerPlayer>())
            if (player.IsSpawned) Register(player);
    }

    public void Register(MultiplayerPlayer player)
    {
        if (Players.Contains(player)) return;
        Players.Add(player);
        if (!IsServer) return;
        int red = 0, blue = 0;
        foreach (var other in Players)
        {
            if (other == player) continue;
            if (other.Team.Value == 0) red++; else blue++;
        }
        player.Team.Value = red <= blue ? 0 : 1;
        player.ServerRespawn();
    }

    public void RecordKill(MultiplayerPlayer victim, MultiplayerPlayer attacker)
    {
        if (!IsServer || Winner.Value >= 0) return;
        victim.Deaths.Value++;
        if (attacker == null || attacker == victim || attacker.Team.Value == victim.Team.Value) return;
        attacker.Kills.Value++;
        if (attacker.Team.Value == 0) RedScore.Value++; else BlueScore.Value++;
        if (RedScore.Value >= scoreLimit || BlueScore.Value >= scoreLimit)
        {
            Winner.Value = RedScore.Value >= scoreLimit ? 0 : 1;
            restartAt = NetworkManager.ServerTime.Time + 10;
        }
    }

    public Pose GetSpawn(int team)
    {
        var points = team == 0 ? redSpawns : blueSpawns;
        if (points == null || points.Length == 0) return new Pose(transform.position + Vector3.up * 2, Quaternion.identity);
        // Prefer the point furthest from living enemies; rotate equal candidates.
        Transform best = points[spawnSequence++ % points.Length];
        float bestDistance = -1;
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[(i + spawnSequence) % points.Length];
            if (!point) continue;
            float nearest = float.MaxValue;
            foreach (var player in Players)
                if (player && player.Team.Value != team && player.HitPoints.Value > 0)
                    nearest = Mathf.Min(nearest, (point.position - player.transform.position).sqrMagnitude);
            if (nearest > bestDistance) { bestDistance = nearest; best = point; }
        }
        return new Pose(best.position, best.rotation);
    }

    private void Update()
    {
        if (!IsSpawned || !IsServer || Winner.Value < 0 || NetworkManager.ServerTime.Time < restartAt) return;
        RedScore.Value = BlueScore.Value = 0;
        Winner.Value = -1;
        foreach (var player in Players)
        {
            if (!player) continue;
            player.Kills.Value = player.Deaths.Value = 0;
            player.ServerRespawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        Players.Clear();
        if (Instance == this) Instance = null;
    }
}
