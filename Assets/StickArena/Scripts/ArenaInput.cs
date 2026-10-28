using Fusion;

public enum ArenaButton { Jump, Fire }

public struct ArenaInput : INetworkInput
{
    public float Move;
    public NetworkButtons Buttons;
}
