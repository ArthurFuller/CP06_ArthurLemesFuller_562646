# Stick Arena

Minigame 3D competitivo para dois jogadores, feito em Unity 6000.3.8f1 e Photon Fusion 2 (Host Mode). O objetivo é empurrar o adversário para os espinhos; cinco pontos encerram a partida.

## Preparação

1. Abra o projeto na Unity 6000.3.8f1. O SDK Fusion 2.1.3 e o App ID já estão no projeto.
2. Abra `Assets/StickArena/Scenes/StickArena.unity` e pressione Play.

## Como jogar

- Para testar sem build, clique em **Testar 2 no Editor**. O Fusion inicia dois peers na mesma sessão.
- Também é possível clicar em **Criar** em um Editor e **Entrar** em outro, usando o mesmo nome da sala.
- Jogador 1: `A/D` mover, `Espaço` pular, `J` disparar.
- Jogador 2 no teste local: setas mover, `Shift direito` pular, `Ctrl direito` disparar.
- Na revanche do teste local, o P1 clica em **Reiniciar** e o P2 aperta `Enter`. Em sessões online, cada um usa o botão na própria tela.
- O feixe empurra o oponente. Quem cair nos espinhos dá um ponto ao adversário; vence quem alcançar cinco pontos.
- Após a vitória, os dois jogadores devem clicar em **Reiniciar** para começar outra partida. **Sair** encerra o Play Mode no Editor.

## Teste multiplayer obrigatório

No Editor, clique em **Testar 2 no Editor** e confira o movimento dos dois personagens, feixe, placar e revanche. Para testar duas telas separadas sem build, use duas cópias do projeto abertas em Editores distintos.

Os objetos visuais, as plataformas, os espinhos e a interface estão salvos na cena ou no prefab do jogador. O jogo só instancia pela rede os jogadores e o estado da partida.

Para ajustar o indicador de direção, abra `Assets/StickArena/Prefabs/Player.prefab` e mova o filho **Direction Marker**. A posição e o tamanho definidos no prefab são preservados; o script apenas espelha o eixo X quando o jogador vira.
