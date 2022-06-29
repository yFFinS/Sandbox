using System.Diagnostics;
using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Checkers.View;

public class AiController : AbstractBoardController
{
    private const long MinMoveTime = 1000;

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

    public readonly BoardHeuristicAnalyzer Analyzer;
    public readonly BoardSolver Solver;

    public AiController()
    {
        Analyzer = new BoardHeuristicAnalyzer();
        Solver = new BoardSolver(Analyzer);
    }

    public override void OnInitialized()
    {
        _ai = new CheckersAi(Board, Solver);
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
            _nextMoveTask = null;
            Board.MakeMove(move.Move);
            IntermediateDisplay.SetMovePathCells(move.Move);
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
}