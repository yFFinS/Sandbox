using Chess.Core;

namespace Chess.View;

public class ControllerVisitor
{
    public bool TurnPassPending { get; private set; }
    public bool GameRestartPending { get; private set; }

    public bool PauseMenuPending { get; private set; }
    //public Move? PendingMove { get; private set; }

    public void PassTurn()
    {
        TurnPassPending = true;
    }

    public void RestartGame()
    {
        GameRestartPending = true;
    }

    public void OpenPauseMenu()
    {
        PauseMenuPending = true;
    }

    public void MakeMove(Move move)
    {
        PendingMove = move;
    }

    public Move? PendingMove { get; private set; }
}