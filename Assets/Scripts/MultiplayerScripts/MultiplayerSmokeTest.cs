#if UNITY_EDITOR || DEBUG
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

/// <summary>Opt-in, three-client integration test. Never runs in ordinary play.</summary>
public class MultiplayerSmokeTest : MonoBehaviour
{
    private string role, output;
    private float started, nextAction;
    private int stage, shots;
    private bool sawWinner;
    private MultiplayerMenu menu;
    private readonly MethodInfo fire = typeof(MultiplayerPlayer).GetMethod("FireRpc", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly MethodInfo reload = typeof(MultiplayerPlayer).GetMethod("ReloadRpc", BindingFlags.Instance | BindingFlags.NonPublic);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        var args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "-multiplayerSmoke");
        if (flag < 0 || flag + 2 >= args.Length) return;
        var test = new GameObject("Multiplayer smoke test").AddComponent<MultiplayerSmokeTest>();
        test.role = args[flag + 1];
        test.output = args[flag + 2];
        test.started = Time.realtimeSinceStartup;
    }

    private void Finish(string result)
    {
        File.WriteAllText(output, result);
        Debug.Log("MULTIPLAYER_SMOKE " + result);
        enabled = false;
        Application.Quit(result.StartsWith("PASS") ? 0 : 1);
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup - started > 100) { Finish("FAIL timeout stage=" + stage); return; }
        if (!menu)
        {
            menu = FindAnyObjectByType<MultiplayerMenu>();
            if (!menu) return;
            menu.address = "127.0.0.1";
            menu.port = 17877;
            if (role == "server") menu.StartServer(); else menu.StartClient();
        }
        var manager = NetworkManager.Singleton;
        var match = TeamDeathmatch.Instance;
        if (!manager || !manager.IsListening || !match || !match.IsSpawned) return;
        if (role == "server")
        {
            match.scoreLimit = 2;
            match.respawnDelay = 1;
            if (match.Winner.Value >= 0) sawWinner = true;
            if (sawWinner && match.Winner.Value == -1)
            {
                if (stage == 0) { stage = 1; nextAction = Time.realtimeSinceStartup + 2; }
                if (Time.realtimeSinceStartup < nextAction) return;
                bool reset = match.RedScore.Value == 0 && match.BlueScore.Value == 0 && match.Players.All(p => p.Kills.Value == 0 && p.Deaths.Value == 0 && p.HitPoints.Value == 100);
                Finish(reset ? "PASS three clients; team assignment, damage, score limit and match reset" : "FAIL match reset");
            }
            return;
        }
        if (!manager.LocalClient.PlayerObject) return;
        var player = manager.LocalClient.PlayerObject.GetComponent<MultiplayerPlayer>();
        // Isolate shooting tests from terrain and keyboard input; movement sync still runs.
        player.enabled = false;
        var body = player.GetComponent<Rigidbody>();
        body.useGravity = false;
        body.linearVelocity = Vector3.zero;
        Vector3 position = player.OwnerClientId == 1 ? new Vector3(0, 500, 0) :
            player.OwnerClientId == 2 ? new Vector3(0, 500, 20) : new Vector3(10, 500, 0);
        if (Vector3.Distance(player.transform.position, position) > 0.1f)
            player.GetComponent<NetworkTransform>().Teleport(position, Quaternion.identity, Vector3.one);
        if (!player.IsOwner || !player.playerCamera.enabled || match.Players.Any(p => p != player && p.playerCamera.enabled))
        { Finish("FAIL camera ownership"); return; }
        if (match.Winner.Value >= 0) sawWinner = true;
        if (sawWinner && match.Winner.Value == -1) { Finish("PASS client ownership and replicated match reset"); return; }
        if (match.Players.Count < 3 || Time.realtimeSinceStartup < nextAction) return;
        // The lowest client ID is the shooter; the other two observe replication.
        if (player.OwnerClientId != match.Players.Min(p => p.OwnerClientId)) return;
        var enemy = match.Players.First(p => p.Team.Value != player.Team.Value);
        var ally = match.Players.FirstOrDefault(p => p != player && p.Team.Value == player.Team.Value);
        if (!ally) { Finish("FAIL balanced team assignment"); return; }
        switch (stage)
        {
            case 0: stage = 1; nextAction = Time.realtimeSinceStartup + 3; break;
            case 1:
                // Move the ally off the enemy's line of fire.
                FireAt(player, ally); stage = 2; nextAction = Time.realtimeSinceStartup + 0.5f; break;
            case 2:
                if (ally.HitPoints.Value != 100 || player.Ammo.Value != 29) { Finish("FAIL friendly fire or server ammo"); return; }
                stage = 3; break;
            case 3:
                if (shots++ < 4) { FireAt(player, enemy); nextAction = Time.realtimeSinceStartup + 0.25f; }
                else { stage = 4; nextAction = Time.realtimeSinceStartup + 0.3f; }
                break;
            case 4:
                if (enemy.HitPoints.Value != 0 || enemy.Deaths.Value != 1 || player.Kills.Value != 1) { Finish("FAIL kill replication"); return; }
                reload.Invoke(player, null); stage = 5; nextAction = Time.realtimeSinceStartup + 2.5f; break;
            case 5:
                if (player.Ammo.Value != 30 || enemy.HitPoints.Value != 100) { Finish("FAIL reload or respawn"); return; }
                shots = 0; stage = 6; break;
            case 6:
                if (shots++ < 4) { FireAt(player, enemy); nextAction = Time.realtimeSinceStartup + 0.25f; }
                else stage = 7;
                break;
        }
    }

    private void FireAt(MultiplayerPlayer player, MultiplayerPlayer target)
    {
        Vector3 direction = (target.transform.position - player.playerCamera.transform.position).normalized;
        fire.Invoke(player, new object[] { direction });
    }
}
#endif
