namespace Chess.Core;

public readonly struct Pin
{
    public int Defender { get; init; }
    public int Attacker { get; init; }
    public int AttackerRay { get; init; }
}