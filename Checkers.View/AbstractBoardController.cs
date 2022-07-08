using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public abstract class AbstractBoardController
{
    protected Board Board { get; private set; } = null!;
    protected BoardDrawable Drawable { get; private set; } = null!;
    protected bool IsMyTurn { get; private set; }

    public void Initialize(Board board, BoardDrawable drawable)
    {
        Board = board;
        Drawable = drawable;
        OnInitialized();
    }

    public virtual void OnInitialized()
    {
    }

    public virtual void OnTurnEnded()
    {
    }

    public void StartGame(bool isMyTurn, PlayerType opponentType)
    {
        IsMyTurn = isMyTurn;
        OnGameStarted(opponentType);
    }

    protected virtual void OnGameStarted(PlayerType opponentType)
    {
    }

    public virtual void OnTurnBegan(MoveInfo opponentMoveInfo)
    {
    }

    public void SetMyTurn(bool isMyTurn, MoveInfo opponentMoveInfo)
    {
        IsMyTurn = isMyTurn;
        if (IsMyTurn)
        {
            OnTurnBegan(opponentMoveInfo);
        }
        else
        {
            OnTurnEnded();
        }
    }

    public abstract void Update(GameTime gameTime, ControllerVisitor visitor);
}