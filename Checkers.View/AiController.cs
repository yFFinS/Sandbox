using System.Diagnostics;
using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Checkers.View;

public class AiController : AbstractBoardController
{
    private const float MinMoveTime = 1000;

    private CheckersAi _ai = null!;
    private bool _gameEnded;
    private MoveSequenceStatus _moveSequenceStatus;

    private enum MoveSequenceStatus
    {
        Idle = 0,
        WaitingForTurn,
        Playing,
        Ready
    }

    private Task<EvaluatedMove>? _nextMoveTask;

    public override void OnInitialized()
    {
        var analyzer = new BoardHeuristicAnalyzer();
        var solver = new BoardSolver(analyzer);
        solver.EnableLogging(Console.Out);

        solver.Configure(config =>
        {
            config.MaxSearchDepth = 20;
            config.MaxEvaluationTime = 5;
            config.UseMultithreading();
        });

        _ai = new CheckersAi(Board, solver);
        _ai.EnableLogging(Console.Out);
    }

    public override void OnTurnEnded()
    {
        IntermediateDisplay.ResetDisplayedMoves();
    }

    public override void OnTurnBegan()
    {
        _gameEnded = Board.IsGameEnded();
        if (_moveSequenceStatus == MoveSequenceStatus.WaitingForTurn)
        {
            _moveSequenceStatus = MoveSequenceStatus.Playing;
            var evaluatedMove = _ai.GetNextMove(true);
            PlayFullMoveSequence(evaluatedMove);
        }
    }

    private void PlayFullMoveSequence(EvaluatedMove evaluatedMove)
    {
        const float delay = 2000;
        Task.Run(async delegate
        {
            var moves = evaluatedMove.FullMoveSequence!;
            foreach (var move in moves.Take(moves.Count % 2 == 1 ? moves.Count : moves.Count - 1))
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delay));
                Board.MakeMove(move);
                IntermediateDisplay.SetMovePathCells(move);
            }

            _moveSequenceStatus = MoveSequenceStatus.Ready;
        });
    }

    public override void Update(GameTime gameTime, ControllerVisitor visitor)
    {
        if (_moveSequenceStatus == MoveSequenceStatus.Playing)
        {
            return;
        }

        if (_nextMoveTask is not null)
        {
            if (!_nextMoveTask.IsCompleted)
            {
                return;
            }

            var move = _nextMoveTask.Result;
            Board.MakeMove(move.Move);
            IntermediateDisplay.SetMovePathCells(move.Move);
            _nextMoveTask = null;

            visitor.PassTurn();
            return;
        }

        if (Input.IsKeyDown(Keys.P) && _moveSequenceStatus == MoveSequenceStatus.Idle)
        {
            _moveSequenceStatus = MoveSequenceStatus.WaitingForTurn;
        }

        if (!IsMyTurn || _gameEnded)
        {
            return;
        }

        if (_moveSequenceStatus == MoveSequenceStatus.Ready)
        {
            _moveSequenceStatus = MoveSequenceStatus.Idle;
            visitor.PassTurn();
            return;
        }

        _nextMoveTask = Task.Run(async delegate
        {
            var startTime = Stopwatch.GetTimestamp();
            var result = await _ai.GetNextMoveAsync();
            var passedTime = (float)(Stopwatch.GetTimestamp() - startTime) / Stopwatch.Frequency;

            if (passedTime < MinMoveTime)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(MinMoveTime - passedTime));
            }

            return result;
        });
    }
}