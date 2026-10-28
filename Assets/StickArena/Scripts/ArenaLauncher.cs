using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ArenaLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkObject playerPrefab, matchPrefab;
    [SerializeField] private string roomName = "StickArena";
    private readonly Dictionary<PlayerRef, NetworkObject> players = new();
    private NetworkRunner runner;
    private ArenaMatch match;
    private static ArenaLauncher primary;
    private bool starting, localTestClient;
    private bool localTestActive;

    public bool IsPrimary => this == primary;
    public bool HideNetworkVisuals => localTestClient;
    public bool Starting => starting;
    public bool LocalTestActive => localTestActive;
    public NetworkRunner Runner => runner;
    public string Status { get; private set; } = "Escolha criar ou entrar";
    public string RoomName { get => roomName; set => roomName = value; }
    public ArenaMatch Match
    {
        get
        {
            if (match != null) return match;
            foreach (var candidate in FindObjectsByType<ArenaMatch>(FindObjectsSortMode.None))
                if (candidate.Runner == runner) return candidate;
            return null;
        }
    }

    private void Awake()
    {
        if (name == "Local Test Client Runner") return;
        if (primary != null && primary != this) enabled = false;
        else primary = this;
    }

    private void Update()
    {
        if (localTestClient && Match != null && Match.Phase == 4 &&
            Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            VoteRestart();
    }

    public async Task<bool> StartSession(GameMode mode)
    {
        if (starting || runner != null) return false;
        starting = true;
        Status = "Conectando...";
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        if (localTestClient) runner.EnableVisibilityExtension();
        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            SessionName = roomName.Trim(),
            PlayerCount = 2,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
        Status = result.Ok ? "Sala " + roomName : "Falha: " + result.ShutdownReason;
        if (result.Ok && localTestClient) runner.SetVisible(false);
        if (!result.Ok) { Destroy(runner); runner = null; }
        starting = false;
        return result.Ok;
    }

    public async void StartLocalTest()
    {
        localTestActive = true;
        NetworkProjectConfig.Global.PeerMode = NetworkProjectConfig.PeerModes.Multiple;
        if (!await StartSession(GameMode.Host)) return;
        var peer = new GameObject("Local Test Client Runner").AddComponent<ArenaLauncher>();
        peer.playerPrefab = playerPrefab;
        peer.matchPrefab = matchPrefab;
        peer.roomName = roomName;
        peer.localTestClient = true;
        await peer.StartSession(GameMode.Client);
    }

    public void VoteRestart() { Match?.RPC_Restart(); }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnPlayerJoined(NetworkRunner r, PlayerRef player)
    {
        if (!r.IsServer) return;
        if (match == null) match = r.Spawn(matchPrefab).GetComponent<ArenaMatch>();
        var position = players.Count == 0 ? new Vector3(-3f, 1f, 0f) : new Vector3(3f, 1f, 0f);
        var avatar = r.Spawn(playerPrefab, position, Quaternion.identity, player);
        players[player] = avatar;
        r.SetPlayerObject(player, avatar);
    }

    public void OnPlayerLeft(NetworkRunner r, PlayerRef player)
    {
        if (!r.IsServer || !players.TryGetValue(player, out var avatar)) return;
        r.Despawn(avatar);
        players.Remove(player);
    }

    public void OnInput(NetworkRunner r, NetworkInput input)
    {
        var keys = Keyboard.current;
        if (keys == null) return;
        var data = new ArenaInput
        {
            Move = ((localTestClient ? keys.rightArrowKey : keys.dKey).isPressed ? 1f : 0f) -
                   ((localTestClient ? keys.leftArrowKey : keys.aKey).isPressed ? 1f : 0f)
        };
        data.Buttons.Set(ArenaButton.Jump, localTestClient ? keys.rightShiftKey.isPressed : keys.spaceKey.isPressed);
        data.Buttons.Set(ArenaButton.Fire, localTestClient ? keys.rightCtrlKey.isPressed : keys.jKey.isPressed);
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner r, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason reason) { Status = "Desconectado: " + reason; runner = null; }
    public void OnConnectedToServer(NetworkRunner r) { Status = "Sala " + roomName; }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { Status = "Desconectado: " + reason; }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress address, NetConnectFailedReason reason) { Status = "Falha: " + reason; }
    public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> sessions) { }
    public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner r, HostMigrationToken token) { }
    public void OnSceneLoadDone(NetworkRunner r) { }
    public void OnSceneLoadStart(NetworkRunner r) { }
    public void OnObjectExitAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner r, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner r, PlayerRef player, ReliableKey key, float progress) { }
}
