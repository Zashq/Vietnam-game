using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiplayerMenu : MonoBehaviour
{
    public NetworkManager manager;
    public Camera lobbyCamera;
    public string address = "127.0.0.1";
    public ushort port = 7777;
    public int maxPlayers = 12;
    public static bool InputBlocked { get; private set; } = true;
    private string status = "Host a match or enter the host's LAN IPv4 address.";
    private bool menuOpen = true;
    private bool connecting;
    private float connectDeadline;

    private void Start()
    {
        if (!manager) manager = GetComponent<NetworkManager>();
        if (!manager) manager = NetworkManager.Singleton;
        if (!manager) { status = "NetworkManager is missing."; return; }
        manager.OnClientConnectedCallback += Connected;
        manager.OnClientDisconnectCallback += Disconnected;
        manager.ConnectionApprovalCallback += Approve;
        manager.NetworkConfig.ConnectionApproval = true;
        Application.runInBackground = true;
        Time.timeScale = 1;
        SetMenu(true);
    }

    private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = manager.ConnectedClientsIds.Count < maxPlayers;
        response.CreatePlayerObject = response.Approved;
        response.Reason = response.Approved ? "" : "The server is full.";
        response.Pending = false;
    }

    private bool Prepare()
    {
        if (!manager || manager.IsListening || connecting) return false;
        var transport = manager.GetComponent<UnityTransport>();
        if (!transport || !manager.NetworkConfig.PlayerPrefab)
        {
            status = "Run Vietnam Game > Create Multiplayer Arena in the Unity editor first.";
            return false;
        }
        if (!System.Net.IPAddress.TryParse(address.Trim(), out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            status = "Enter a valid IPv4 address, for example 192.168.1.20.";
            return false;
        }
        manager.NetworkConfig.NetworkTransport = transport;
        transport.SetConnectionData(address.Trim(), port, "0.0.0.0");
        return true;
    }

    public void StartHost()
    {
        if (!Prepare()) return;
        status = manager.StartHost() ? "Hosting on port " + port : "Could not start host. Is the port already in use?";
    }

    public void StartClient()
    {
        if (!Prepare()) return;
        connecting = manager.StartClient();
        connectDeadline = Time.realtimeSinceStartup + 15;
        status = connecting ? "Connecting to " + address + ":" + port + "..." : "Could not start client.";
    }

    public void StartServer()
    {
        if (!Prepare()) return;
        status = manager.StartServer() ? "Server running on port " + port : "Could not start server.";
    }

    private void Connected(ulong clientId)
    {
        if (clientId != manager.LocalClientId || !manager.IsClient) return;
        connecting = false;
        status = "Connected";
        SetMenu(false);
    }

    private void Disconnected(ulong clientId)
    {
        if (manager.IsServer && clientId != manager.LocalClientId) return;
        connecting = false;
        status = string.IsNullOrEmpty(manager.DisconnectReason) ? "Disconnected from server." : manager.DisconnectReason;
        SetMenu(true);
    }

    public void Leave()
    {
        connecting = false;
        if (manager) manager.Shutdown();
        status = "Disconnected. You can host or join another match.";
        SetMenu(true);
    }

    private void SetMenu(bool open)
    {
        menuOpen = open;
        InputBlocked = open;
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;
    }

    private void Update()
    {
        if (!manager) return;
        bool hasPlayer = manager.IsClient && manager.LocalClient != null && manager.LocalClient.PlayerObject;
        if (lobbyCamera)
        {
            lobbyCamera.enabled = !hasPlayer;
            var listener = lobbyCamera.GetComponent<AudioListener>();
            if (listener) listener.enabled = !hasPlayer;
        }
        if (connecting && Time.realtimeSinceStartup >= connectDeadline)
        {
            Leave();
            status = "Connection timed out. Check the host's IP, port and firewall.";
        }
        if (hasPlayer && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) SetMenu(!menuOpen);
        // Multiplayer menus block only local input; simulation keeps running.
    }

    private void OnGUI()
    {
        if (!manager) return;
        var player = manager.IsClient && manager.LocalClient != null && manager.LocalClient.PlayerObject
            ? manager.LocalClient.PlayerObject.GetComponent<MultiplayerPlayer>() : null;
        var match = TeamDeathmatch.Instance;
        if (player && match)
        {
            GUI.Box(new Rect(15, 15, 370, 95), "TEAM DEATHMATCH");
            GUI.Label(new Rect(30, 40, 340, 25), $"Red {match.RedScore.Value}  :  {match.BlueScore.Value} Blue     First to {match.scoreLimit}");
            GUI.Label(new Rect(30, 65, 340, 25), $"Team {(player.Team.Value == 0 ? "Red" : "Blue")}    HP {player.HitPoints.Value}    Ammo {player.Ammo.Value}/{player.magazineSize}");
            if (player.Reloading.Value) GUI.Label(new Rect(Screen.width / 2 - 50, Screen.height / 2 + 35, 200, 30), "Reloading...");
            if (match.Winner.Value >= 0)
                GUI.Box(new Rect(Screen.width / 2 - 180, 130, 360, 50), (match.Winner.Value == 0 ? "RED" : "BLUE") + " WINS!\nNew match starts shortly.");
            else if (player.HitPoints.Value <= 0)
                GUI.Box(new Rect(Screen.width / 2 - 130, 130, 260, 40), "You died. Respawning...");
            else if (!menuOpen) GUI.Label(new Rect(Screen.width / 2 - 5, Screen.height / 2 - 10, 20, 20), "+");
            if (Keyboard.current != null && Keyboard.current.tabKey.isPressed)
            {
                GUILayout.BeginArea(new Rect(Screen.width - 330, 15, 315, 420), GUI.skin.box);
                GUILayout.Label("SCOREBOARD      Kills / Deaths");
                foreach (var other in match.Players)
                    if (other) GUILayout.Label($"{(other.Team.Value == 0 ? "Red" : "Blue")}  Player {other.OwnerClientId}       {other.Kills.Value} / {other.Deaths.Value}");
                GUILayout.EndArea();
            }
        }
        if (!menuOpen) return;
        GUILayout.BeginArea(new Rect(Screen.width / 2 - 210, Screen.height / 2 - 160, 420, 320), GUI.skin.box);
        GUILayout.Label("VIETNAM — LAN TEAM DEATHMATCH");
        GUILayout.Label(status);
        if (!manager.IsListening && !connecting && !manager.ShutdownInProgress)
        {
            GUILayout.Label("Host IPv4 address (127.0.0.1 for this computer)");
            address = GUILayout.TextField(address, 45);
            GUILayout.Label("UDP port: " + port);
            if (GUILayout.Button("Host match")) StartHost();
            if (GUILayout.Button("Join match")) StartClient();
            if (GUILayout.Button("Start server only")) StartServer();
        }
        else
        {
            if (player && GUILayout.Button("Resume")) SetMenu(false);
            if (GUILayout.Button("Disconnect / stop server")) Leave();
        }
        GUILayout.Label("WASD move · Shift sprint · Space jump\nLMB fire · RMB zoom · R reload · Tab scores · Esc menu");
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (manager)
        {
            manager.OnClientConnectedCallback -= Connected;
            manager.OnClientDisconnectCallback -= Disconnected;
            manager.ConnectionApprovalCallback -= Approve;
        }
        InputBlocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
