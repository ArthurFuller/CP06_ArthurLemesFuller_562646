using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ArenaUI : MonoBehaviour
{
    private static ArenaUI primary;
    [SerializeField] private ArenaLauncher launcher;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject menu, hud, banner, finish;
    [SerializeField] private InputField room;
    [SerializeField] private Text status, redScore, blueScore, bannerText, finishText, readyText;
    [SerializeField] private Button create, join, restart, exit;

    private void Awake()
    {
        if (primary != null && primary != this) { Destroy(gameObject); return; }
        primary = this;
        DontDestroyOnLoad(gameObject);
        create.onClick.AddListener(() => _ = launcher.StartSession(GameMode.Host));
        join.onClick.AddListener(() => _ = launcher.StartSession(GameMode.Client));
        restart.onClick.AddListener(launcher.VoteRestart);
        exit.onClick.AddListener(launcher.Exit);
        room.text = launcher.RoomName;
        room.onValueChanged.AddListener(value => launcher.RoomName = value);
    }

    private void Update()
    {
        canvas.enabled = launcher.IsPrimary;
        if (!launcher.IsPrimary) return;
        bool connected = launcher.Runner != null && launcher.Runner.IsRunning;
        menu.SetActive(!connected);
        hud.SetActive(connected);
        status.text = launcher.Status;
        if (!connected) return;

        var match = launcher.Match;
        redScore.text = match == null ? "0" : match.RedScore.ToString();
        blueScore.text = match == null ? "0" : match.BlueScore.ToString();
        banner.SetActive(match == null || match.Phase != 2 && match.Phase != 4);
        finish.SetActive(match != null && match.Phase == 4);
        if (match == null || match.Phase == 0) bannerText.text = "AGUARDANDO OUTRO JOGADOR";
        else if (match.Phase == 1) bannerText.text = $"FAÇA {match.Goal} PONTOS\n{match.SecondsLeft}";
        else if (match.Phase == 3) bannerText.text = match.LastWinner == 1 ? "PONTO DO VERMELHO!" : "PONTO DO AZUL!";
        if (match == null || match.Phase != 4) return;
        finishText.text = match.LastWinner == 1 ? "VERMELHO VENCEU!" : "AZUL VENCEU!";
        readyText.text = $"REINICIAR  {(match.RedReady ? 1 : 0) + (match.BlueReady ? 1 : 0)}/2";
        bool voted = match.RedPlayer != null && match.RedPlayer.InputAuthority == launcher.Runner.LocalPlayer
            ? match.RedReady : match.BlueReady;
        restart.interactable = !voted;
    }
}
