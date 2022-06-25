namespace Checkers;

public static class Program
{
    [MTAThread]
    public static void Main(string[] args)
    {
        var game = new GameMain(args);
        game.Run();
    }
}