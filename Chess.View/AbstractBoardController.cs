﻿using Checkers.View;
using Chess.Core;   
using Microsoft.Xna.Framework;

namespace Chess.View;

public abstract class AbstractBoardController
{
    protected ChessBoard Board { get; private set; } = null!;
    protected BoardDrawable Drawable { get; private set; } = null!;
    protected bool IsMyTurn { get; private set; }

    public void Initialize(ChessBoard board, BoardDrawable drawable)
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