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
    [Networked] private TickTimer Timer { get; set; }

    public int Goal => pointsToWin;
    public int SecondsLeft => Mathf.CeilToInt(Timer.RemainingTime(Runner) ?? 0f);

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        ArenaPlayer red = null, blue = null;
        foreach (var player in Runner.ActivePlayers)
        {
            var obj = Runner.GetPlayerObject(player);
            if (obj == null) continue;
            if (player.RawEncoded == 1) red = obj.GetComponent<ArenaPlayer>();
            else blue = obj.GetComponent<ArenaPlayer>();
        }
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
        red.ResetForRound(new Vector3(-3f, 1f, 0f));
        blue.ResetForRound(new Vector3(3f, 1f, 0f));
        Phase = 1;
        Timer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void RPC_Restart(RpcInfo info = default)
    {
        if (Phase != 4 || Runner.GetPlayerObject(info.Source) == null) return;
        if (info.Source.RawEncoded == 1) RedReady = true;
        else BlueReady = true;
    }
}
