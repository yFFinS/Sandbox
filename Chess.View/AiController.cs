﻿using Checkers.View;
using Chess.Core;
using Chess.Core.Solver;
using Microsoft.Xna.Framework;

namespace Chess.View;

public class AiController : AbstractBoardController
{
    private MoveAnimator _moveAnimator = null!;
    private bool _isWaitingForAnimationFinish;
    private Move? _finalMove;

    private ChessAi _ai = null!;
    private bool _isWaitingForAiMove;

    public override void OnInitialized()
    {
        _moveAnimator = new MoveAnimator(Board, Drawable);
        var solver = new ChessBoardSolver(new ChessBoardHeuristicAnalyzer());
        solver.Configure(config =>
        {
            config.MaxEvaluationTime = 5;
            config.HardSearchDepthCap = 15;
        });
        _ai = new ChessAi(Board, solver);
        solver.EnableLogging(Console.Out);
    }

    public void ScheduleMoveAnimation(Move move)
    {
        _moveAnimator.AnimateMove(move);
        _isWaitingForAnimationFinish = true;
        _finalMove = move;
    }

    protected override void OnGameStarted(PlayerType opponentType)
    {
        _finalMove = null;
        _isWaitingForAiMove = false;
        _isWaitingForAnimationFinish = false;
    }

    public override void Update(GameTime gameTime, ControllerVisitor visitor)
    {
        if (Board.GetGameEndState() != GameEndState.None)
        {
            return;
        }
        
        _moveAnimator.Update(gameTime);

        if (_isWaitingForAiMove)
        {
            if (_finalMove is null)
            {
                return;
            }

            ScheduleMoveAnimation(_finalMove.Value);
            _isWaitingForAiMove = false;
        }

        if (_isWaitingForAnimationFinish)
        {
            if (_moveAnimator.WaitingForEndConfirm)
            {
                EndAnimationAndMakeMove(_finalMove!.Value, visitor);
                _finalMove = null;
            }

            return;
        }

        if (IsMyTurn)
        {
            _isWaitingForAiMove = true;
            Task.Run(() =>
            {
                _finalMove = _ai.GetNextMove();
                return Task.CompletedTask;
            });
        }
    }

    private void EndAnimationAndMakeMove(Move finalMove, ControllerVisitor visitor)
    {
        _moveAnimator.ConfirmEnding();
        _isWaitingForAnimationFinish = false;
        visitor.MakeMove(finalMove);
        visitor.PassTurn();
    }
}