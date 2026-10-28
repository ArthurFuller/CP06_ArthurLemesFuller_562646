using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ArenaUI : MonoBehaviour
{
    [SerializeField] private ArenaLauncher launcher;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GameObject menu, hud, banner, finish;
    [SerializeField] private InputField room;
    [SerializeField] private Text status, redScore, blueScore, bannerText, finishText, readyText, controls;
    [SerializeField] private Button create, join, localTest, restart, exit;

    private void Awake()
    {
        create.onClick.AddListener(() => _ = launcher.StartSession(GameMode.Host));
        join.onClick.AddListener(() => _ = launcher.StartSession(GameMode.Client));
        localTest.onClick.AddListener(launcher.StartLocalTest);
        restart.onClick.AddListener(launcher.VoteRestart);
        exit.onClick.AddListener(launcher.Exit);
        room.text = launcher.RoomName;
        room.onValueChanged.AddListener(value => launcher.RoomName = value);
        localTest.gameObject.SetActive(Application.isEditor);
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

        controls.text = launcher.LocalTestActive
            ? "P1  A/D MOVER  •  ESPAÇO PULAR  •  J ATIRAR     |     P2  SETAS MOVER  •  SHIFT DIR. PULAR  •  CTRL DIR. ATIRAR"
            : "A/D MOVER     •     ESPAÇO PULAR     •     J ATIRAR";
        var match = launcher.Match;
        redScore.text = match == null ? "0" : match.RedScore.ToString();
        blueScore.text = match == null ? "0" : match.BlueScore.ToString();
        banner.SetActive(match == null || match.Phase != 2 && match.Phase != 4);
        finish.SetActive(match != null && match.Phase == 4);
        if (match == null || match.Phase == 0) bannerText.text = "AGUARDANDO OUTRO JOGADOR";
        else if (match.Phase == 1) bannerText.text = $"PRIMEIRO A {match.Goal} PONTOS\n{match.SecondsLeft}";
        else if (match.Phase == 3) bannerText.text = match.LastWinner == 1 ? "PONTO DO VERMELHO!" : "PONTO DO AZUL!";
        if (match == null || match.Phase != 4) return;
        finishText.text = match.LastWinner == 1 ? "VERMELHO VENCEU!" : "AZUL VENCEU!";
        readyText.text = $"REINICIAR  {(match.RedReady ? 1 : 0) + (match.BlueReady ? 1 : 0)}/2" +
                         (launcher.LocalTestActive ? "  •  P2: ENTER" : "");
        bool voted = launcher.Runner.LocalPlayer.RawEncoded == 1 ? match.RedReady : match.BlueReady;
        restart.interactable = !voted;
    }
}
