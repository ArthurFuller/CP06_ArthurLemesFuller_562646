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
    private bool starting;

    public bool IsPrimary => this == primary;
    public NetworkRunner Runner => runner;
    public string Status { get; private set; } = "";
    public string RoomName { get => roomName; set => roomName = value; }
    public ArenaMatch Match
    {
        get
        {
            if (match != null) return match;
            ArenaMatch fallback = null;
            foreach (var candidate in FindObjectsByType<ArenaMatch>(FindObjectsSortMode.None))
            {
                if (candidate.Runner == runner) return match = candidate;
                fallback ??= candidate;
            }
            return fallback;
        }
    }

    private void Awake()
    {
        if (primary != null && primary != this) enabled = false;
        else { primary = this; DontDestroyOnLoad(gameObject); }
    }

    public async Task<bool> StartSession(GameMode mode)
    {
        if (starting || runner != null) return false;
        starting = true;
        Status = "Conectando...";
        GameObject runnerObject = null;
        try
        {
            runnerObject = new GameObject("Network Runner");
            runner = runnerObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;
            runner.AddCallbacks(this);
            var connection = runner.StartGame(new StartGameArgs
            {
                GameMode = mode,
                SessionName = roomName.Trim(),
                PlayerCount = 2,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>()
            });
            if (await Task.WhenAny(connection, Task.Delay(30000)) != connection)
                throw new TimeoutException("tempo de conexão esgotado (30 s)");
            var result = await connection;
            Status = result.Ok ? "" : "Não foi possível conectar";
            if (!result.Ok) { Destroy(runnerObject); runner = null; }
            return result.Ok;
        }
        catch (Exception error)
        {
            Status = "Não foi possível conectar";
            Debug.LogException(error);
            Destroy(runnerObject);
            runner = null;
            return false;
        }
        finally { starting = false; }
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
        bool isRed = match.RedPlayer == null;
        var position = ArenaMatch.SpawnPosition(isRed ? 1 : 2);
        var avatar = r.Spawn(playerPrefab, position, Quaternion.identity, player);
        avatar.GetComponent<ArenaPlayer>().Team = isRed ? 1 : 2;
        if (isRed) match.RedPlayer = avatar;
        else match.BluePlayer = avatar;
        players[player] = avatar;
        r.SetPlayerObject(player, avatar);
    }

    public void OnPlayerLeft(NetworkRunner r, PlayerRef player)
    {
        if (!r.IsServer || !players.TryGetValue(player, out var avatar)) return;
        r.Despawn(avatar);
        players.Remove(player);
        if (match != null)
        {
            if (match.RedPlayer == avatar) match.RedPlayer = null;
            if (match.BluePlayer == avatar) match.BluePlayer = null;
        }
    }

    public void OnInput(NetworkRunner r, NetworkInput input)
    {
        var keys = Keyboard.current;
        if (keys == null) return;
        var data = new ArenaInput
        {
            Move = (keys.dKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed ? 1f : 0f)
        };
        data.Buttons.Set(ArenaButton.Jump, keys.spaceKey.isPressed);
        data.Buttons.Set(ArenaButton.Fire, keys.jKey.isPressed);
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner r, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner r, ShutdownReason reason)
    {
        runner = null;
        match = null;
        players.Clear();
    }
    public void OnConnectedToServer(NetworkRunner r) { }
    public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner r, NetAddress address, NetConnectFailedReason reason) { }
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
