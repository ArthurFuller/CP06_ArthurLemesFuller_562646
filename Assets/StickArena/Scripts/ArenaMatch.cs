using Fusion;
using UnityEngine;

public class ArenaMatch : NetworkBehaviour
{
    [SerializeField] private int pointsToWin = 5;
    [Networked] public int RedScore { get; set; }
    [Networked] public int BlueScore { get; set; }
    [Networked] public int Phase { get; set; } // 0 espera, 1 contagem, 2 jogo, 3 ponto, 4 fim
    [Networked] public int LastWinner { get; set; }
    [Networked] public bool RedReady { get; set; }
    [Networked] public bool BlueReady { get; set; }
    [Networked] public NetworkObject RedPlayer { get; set; }
    [Networked] public NetworkObject BluePlayer { get; set; }
    [Networked] private TickTimer Timer { get; set; }

    public int Goal => pointsToWin;
    public int SecondsLeft => Mathf.CeilToInt(Timer.RemainingTime(Runner) ?? 0f);
    public static Vector3 SpawnPosition(int player) => new(player == 1 ? -3.5f : 3.5f, 2.22f, 0f);

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        var red = RedPlayer != null ? RedPlayer.GetComponent<ArenaPlayer>() : null;
        var blue = BluePlayer != null ? BluePlayer.GetComponent<ArenaPlayer>() : null;
        if (red == null || blue == null) { Phase = 0; return; }

        if (Phase == 4)
        {
            if (RedReady && BlueReady)
            {
                RedScore = BlueScore = 0;
                RedReady = BlueReady = false;
                StartCountdown(red, blue);
            }
            return;
        }
        if (Phase == 0 || (Phase == 3 && Timer.Expired(Runner)))
            StartCountdown(red, blue);
        else if (Phase == 1 && Timer.Expired(Runner)) Phase = 2;
        else if (Phase == 2)
        {
            int loser = red.transform.position.y < -3f ? 1 : blue.transform.position.y < -3f ? 2 : 0;
            if (loser == 0) return;
            (loser == 1 ? red : blue).SetEliminated(true);
            LastWinner = loser == 1 ? 2 : 1;
            if (LastWinner == 1) RedScore++; else BlueScore++;
            Phase = RedScore >= pointsToWin || BlueScore >= pointsToWin ? 4 : 3;
            Timer = TickTimer.CreateFromSeconds(Runner, 2f);
        }
    }

    private void StartCountdown(ArenaPlayer red, ArenaPlayer blue)
    {
        red.ResetForRound(SpawnPosition(1));
        blue.ResetForRound(SpawnPosition(2));
        Phase = 1;
        Timer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_Restart(RpcInfo info = default)
    {
        if (Phase != 4) return;
        if (RedPlayer != null && info.Source == RedPlayer.InputAuthority) RedReady = true;
        if (BluePlayer != null && info.Source == BluePlayer.InputAuthority) BlueReady = true;
    }
}
