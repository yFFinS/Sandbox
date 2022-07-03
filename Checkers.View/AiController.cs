using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

public class AiController : AbstractBoardController
{
    private const long MinMoveTime = 1000;

    private CheckersAi _ai = null!;
    private bool _gameEnded;

    private Task<EvaluatedMove>? _nextMoveTask;

    public readonly BoardHeuristicAnalyzer Analyzer;
    public readonly BoardSolver Solver;

    private MoveAnimator _moveAnimator = null!;

    public AiController()
    {
        Analyzer = new BoardHeuristicAnalyzer();
        Solver = new BoardSolver(Analyzer);
    }

    public override void OnInitialized()
    {
        _ai = new CheckersAi(Board, Solver);
        _ai.EnableLogging(Console.Out);
        _ai.Solver.EnableLogging(Console.Out);

        _moveAnimator = new MoveAnimator(Drawable);
    }

    public override void OnTurnBegan(MoveInfo opponentMoveInfo)
    {
        _gameEnded = Board.IsGameEnded();
        _ai.SelectMove(opponentMoveInfo.Move);
    }

    protected override void OnGameStarted()
    {
        _ai.OnGameStarted();
    }

    public override void Update(GameTime gameTime, ControllerVisitor visitor)
    {
        _moveAnimator.Update(gameTime);

        if (_moveAnimator.Animating)
        {
            return;
        }

        if (_moveAnimator.WaitingForEndingConfirm)
        {
            MakeMove(visitor);
            return;
        }

        if (_nextMoveTask is not null)
        {
            if (!_nextMoveTask.IsCompleted)
            {
                return;
            }

            var move = _nextMoveTask.Result;

            _moveAnimator.AnimateMove(Board.MoveGenerator.GetMoveInfo(move.Move));
            return;
        }

        if (!IsMyTurn || _gameEnded)
        {
            return;
        }

        _nextMoveTask = Task.Run(async () =>
        {
            var startTime = Stopwatch.StartNew();
            var result = await _ai.GetNextMoveAsync();
            var passedTime = startTime.ElapsedMilliseconds;

            if (passedTime < MinMoveTime)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(MinMoveTime - passedTime));
            }

            return result;
        });
    }

    private void MakeMove(ControllerVisitor visitor)
    {
        _moveAnimator.ConfirmEnding();

        var move = _nextMoveTask!.Result;
        _nextMoveTask = null;

        _ai.SelectMove(move.Move);
        visitor.MakeMove(move.Move);
        visitor.PassTurn();
    }
}