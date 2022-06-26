using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public abstract class AbstractBoardController
{
    protected Board Board { get; private set; } = null!;
    protected BoardIntermediateDisplay IntermediateDisplay { get; private set; } = null!;
    protected bool IsMyTurn { get; private set; }

    public void Initialize(Board board, BoardIntermediateDisplay intermediateDisplay)
    {
        Board = board;
        IntermediateDisplay = intermediateDisplay;
        OnInitialized();
    }

    public virtual void OnInitialized()
    {
    }

    public virtual void OnTurnEnded()
    {
    }

    public virtual void OnTurnBegan()
    {
    }

    public void SetMyTurn(bool isMyTurn)
    {
        IsMyTurn = isMyTurn;
        if (IsMyTurn)
        {
            OnTurnBegan();
        }
        else
        {
            OnTurnEnded();
        }
    }

    public abstract void Update(GameTime gameTime, ControllerVisitor visitor);
}