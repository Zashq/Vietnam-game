using System;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds multiplayer assets from a copy of the existing map.</summary>
public static class MultiplayerArenaSetup
{
    public const string ScenePath = "Assets/Scenes/MultiplayerArena.unity";
    private const string PrefabPath = "Assets/Multiplayer/MultiplayerPlayer.prefab";
    private const string RequestPath = "Temp/CreateMultiplayerArena.request";

    [InitializeOnLoadMethod]
    private static void CheckRequestedSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(RequestPath) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(RequestPath);
            try { Create(); File.WriteAllText("Temp/MultiplayerArenaSetup.result", "SUCCESS"); }
            catch (Exception exception)
            {
                File.WriteAllText("Temp/MultiplayerArenaSetup.result", exception.ToString());
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("Vietnam Game/Create Multiplayer Arena")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before setting up multiplayer.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
        {
            Debug.Log("MultiplayerArena already exists. Open it from Assets/Scenes.");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Multiplayer")) AssetDatabase.CreateFolder("Assets", "Multiplayer");
        if (!AssetDatabase.CopyAsset("Assets/Scenes/SampleScene.unity", ScenePath)) throw new IOException("Could not copy SampleScene.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var roots = scene.GetRootGameObjects();
            var source = roots.FirstOrDefault(go => go.name == "MainPlayer");
            if (!source) throw new InvalidOperationException("MainPlayer was not found in SampleScene.");
            var initialPosition = source.transform.position;
            var camera = source.GetComponentInChildren<Camera>(true);
            if (!camera) throw new InvalidOperationException("MainPlayer needs a child Camera.");
            var gun = source.GetComponentInChildren<WPN_AKM>(true);
            var holder = source.GetComponentInChildren<WeaponManager>(true);
            var muzzle = gun ? gun.muzzlePoint : null;
            var weaponRoot = holder ? holder.gameObject : null;
            var red = CreateMaterial("RedTeam", new Color(0.8f, 0.12f, 0.08f));
            var blue = CreateMaterial("BlueTeam", new Color(0.1f, 0.35f, 0.9f));
            // Unpack model instances so removing prototype scripts does not leave prefab overrides.
            foreach (var child in source.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                    PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var behaviour in source.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!behaviour) continue;
                if (behaviour is NetworkObject || behaviour is NetworkTransform) continue;
                if (behaviour.GetType().Assembly == typeof(MultiplayerPlayer).Assembly)
                    UnityEngine.Object.DestroyImmediate(behaviour);
            }
            // Held weapon colliders must not obstruct bullets or player movement.
            foreach (var collider in source.GetComponentsInChildren<Collider>(true))
                if (collider.gameObject != source) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var rigidbody in source.GetComponentsInChildren<Rigidbody>(true))
                if (rigidbody.gameObject != source) UnityEngine.Object.DestroyImmediate(rigidbody);
            var networkObject = source.GetComponent<NetworkObject>() ?? source.AddComponent<NetworkObject>();
            var sync = source.GetComponent<NetworkTransform>() ?? source.AddComponent<NetworkTransform>();
            sync.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
            sync.Interpolate = true;
            sync.SyncScaleX = sync.SyncScaleY = sync.SyncScaleZ = false;
            sync.SyncRotAngleX = sync.SyncRotAngleZ = false;
            var player = source.AddComponent<MultiplayerPlayer>();
            player.playerCamera = camera;
            player.muzzle = muzzle;
            player.firstPersonWeapons = weaponRoot;
            player.bodyRenderer = source.GetComponent<Renderer>();
            player.redMaterial = red;
            player.blueMaterial = blue;
            if (weaponRoot)
                for (int i = 0; i < weaponRoot.transform.childCount; i++) weaponRoot.transform.GetChild(i).gameObject.SetActive(i == 0);
            source.name = "MultiplayerPlayer";
            source.transform.position = Vector3.zero;
            source.transform.rotation = Quaternion.identity;
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            UnityEngine.Object.DestroyImmediate(source);

            // Remove the single-player HUD and disable prototype gameplay on world objects.
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    canvas.gameObject.SetActive(false);
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (behaviour && behaviour.GetType().Assembly == typeof(MultiplayerPlayer).Assembly)
                        behaviour.enabled = false;
            }
            var manager = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<NetworkManager>(true)).FirstOrDefault();
            if (!manager) manager = new GameObject("MultiplayerManager").AddComponent<NetworkManager>();
            var transport = manager.GetComponent<UnityTransport>() ?? manager.gameObject.AddComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = prefab;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.TickRate = 30;
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            var lobby = manager.gameObject.GetComponent<MultiplayerMenu>() ?? manager.gameObject.AddComponent<MultiplayerMenu>();
            lobby.enabled = true;
            lobby.manager = manager;
            var lobbyView = new GameObject("Lobby Camera");
            lobbyView.transform.SetPositionAndRotation(initialPosition + new Vector3(0, 8, -8), Quaternion.Euler(25, 0, 0));
            lobby.lobbyCamera = lobbyView.AddComponent<Camera>();
            lobbyView.AddComponent<AudioListener>();

            var matchObject = new GameObject("Team Deathmatch");
            matchObject.AddComponent<NetworkObject>();
            var match = matchObject.AddComponent<TeamDeathmatch>();
            match.redSpawns = CreateSpawns(scene, "Red Spawns", initialPosition, -12, 0);
            match.blueSpawns = CreateSpawns(scene, "Blue Spawns", initialPosition, 12, 180);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            var builds = EditorBuildSettings.scenes.ToList();
            if (!builds.Any(item => item.path == ScenePath)) builds.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Created MultiplayerArena and player prefab. Open Assets/Scenes/MultiplayerArena.unity and press Play. Use File > Build Profiles to build a second instance.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = "Assets/Multiplayer/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Transform[] CreateSpawns(Scene scene, string name, Vector3 origin, float offset, float yaw)
    {
        var parent = new GameObject(name);
        var terrain = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<Terrain>()).FirstOrDefault();
        var points = new Transform[4];
        for (int i = 0; i < points.Length; i++)
        {
            var position = origin + new Vector3((i - 1.5f) * 4, 2, offset);
            if (terrain && terrain.terrainData) position.y = Mathf.Max(position.y, terrain.SampleHeight(position) + terrain.transform.position.y + 2);
            var point = new GameObject(name + " " + (i + 1)).transform;
            point.SetParent(parent.transform);
            point.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            points[i] = point;
        }
        return points;
    }
}
