using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Checkers.View;

public class PlayerController : AbstractBoardController
{
    private readonly Stack<BoardState> _gameHistory = new();

    private enum PlayerMode
    {
        Playing = 0,
        Editing
    }

    private PlayerMode _playerMode;
    private bool _gameEnded;
    private MoveFullInfo[]? _currentMoves;
    private MoveFullInfo[]? _partialMoves;
    private int _partialPathIndex;

    public override void OnTurnBegan()
    {
        _gameEnded = Board.IsGameEnded();
    }

    public override void Update(GameTime gameTime, ControllerVisitor visitor)
    {
        if (Input.IsKeyDown(Keys.M))
        {
            _playerMode = _playerMode == PlayerMode.Playing ? PlayerMode.Editing : PlayerMode.Playing;
            return;
        }

        if (Input.IsButtonDown(0) && _gameEnded)
        {
            RestartGame(visitor);
            return;
        }

        if (!IsMyTurn)
        {
            return;
        }

        if (_playerMode == PlayerMode.Editing)
        {
            var leftClick = Input.IsButtonDown(0);
            var rightClick = Input.IsButtonDown(1);

            if (!leftClick && !rightClick)
            {
                return;
            }

            var cell = IntermediateDisplay.GetCellAt(Input.MousePosition);
            if (!Board.IsInBounds(cell))
            {
                return;
            }

            var color = leftClick ? PieceColor.White : PieceColor.Black;
            var piece = Board.GetPieceAt(cell);

            if (piece.IsEmpty)
            {
                Board.SetPieceAt(cell, new Piece(PieceType.Pawn, color));
            }
            else if (piece.Type == PieceType.Queen || piece.Color != color)
            {
                Board.SetPieceAt(cell, Piece.Empty);
            }
            else
            {
                Board.SetPieceAt(cell, new Piece(PieceType.Queen, color));
            }

            return;
        }

        if (Input.IsKeyDown(Keys.Left))
        {
            TryRollbackGameState();
        }
        else if (Input.IsKeyDown(Keys.Escape))
        {
            RestartGame(visitor);
        }
        else if (Input.IsButtonDown(0))
        {
            var cellPosition = IntermediateDisplay.GetCellAt(Input.MousePosition);
            HandleCellClick(cellPosition, visitor);
        }
        else if (Input.IsButtonDown(1))
        {
            ResetMoves();
        }
    }

    private void RestartGame(ControllerVisitor visitor)
    {
        Board.Reset();
        ResetMoves();

        _playerMode = PlayerMode.Playing;
        _gameEnded = false;
        IntermediateDisplay.ResetMovePathCells();

        visitor.RestartGame();
    }

    private void TryRollbackGameState()
    {
        if (_gameHistory.Count == 0)
        {
            return;
        }

        Board.SetState(_gameHistory.Pop());
        IntermediateDisplay.ResetMovePathCells();
        ResetMoves();
    }

    private void UpdateDisplayedMoves()
    {
        var moves = new List<MoveDisplayInfo>();
        if (_currentMoves is not null)
        {
            foreach (var moveFullInfo in _currentMoves)
            {
                moves.Add(new MoveDisplayInfo
                {
                    PathColor = Color.Green,
                    FullInfo = moveFullInfo,
                    PieceColor = new Color(Color.Green, 25)
                });
            }
        }

        IntermediateDisplay.ResetPartialMove();
        IntermediateDisplay.SetDisplayedAvailableMoves(moves);
    }

    private void MakeMove(Move move, ControllerVisitor visitor)
    {
        ResetMoves();
        _gameHistory.Push(Board.GetState());

        Board.MakeMove(move);
        _gameEnded = Board.IsGameEnded();

        IntermediateDisplay.ResetSelectedCell();
        IntermediateDisplay.SetMovePathCells(move);

        visitor.PassTurn();
    }

    private void TryMakeMove(Position position, ControllerVisitor visitor)
    {
        var moves = _partialMoves ?? _currentMoves;
        var collisions = moves!.Where(move => move.Move.Path.Contains(position)).ToArray();

        switch (collisions.Length)
        {
            case 0:
                UpdateAvailableMoves(position);
                return;
            case 1:
            {
                var move = collisions[0];
                if (move.Move.Path[^1] == position)
                {
                    MakeMove(move.Move, visitor);
                    return;
                }
            }
                break;
            default:
                var endPositionsAreSame = collisions.Select(col => col.EndPosition).AllEqual();
                var pathCapturesAreSameInAnyOrder = collisions.Select(col =>
                        col.CapturedPositions.OrderBy(pos => (pos.X, pos.Y)))
                    .AllSequencesEqual();
                if (endPositionsAreSame && pathCapturesAreSameInAnyOrder)
                {
                    var move = collisions[0];
                    MakeMove(move.Move, visitor);
                    return;
                }

                var moveWithOnlyOneStep =
                    collisions.FirstOrDefault(col => col.EndPosition == position && col.Move.Path.Count == 1);
                if (moveWithOnlyOneStep is not null)
                {
                    MakeMove(moveWithOnlyOneStep.Move, visitor);
                    return;
                }

                break;
        }

        UpdatePartialMoves(position, collisions);
        DisplayPartialMove();
    }

    private void UpdatePartialMoves(Position position, IReadOnlyList<MoveFullInfo> collisions)
    {
        _partialPathIndex = collisions.Min(col => Array.IndexOf(col.Move.Path.ToArray(), position));
        _partialMoves = collisions
            .Where(m => m.Move.Path[_partialPathIndex] == position)
            .ToArray();
    }

    private void HandleCellClick(Position position, ControllerVisitor visitor)
    {
        if (!Board.IsInBounds(position))
        {
            return;
        }

        if (_currentMoves is null)
        {
            UpdateAvailableMoves(position);
        }
        else
        {
            TryMakeMove(position, visitor);
        }

        if (_currentMoves is not null)
        {
            IntermediateDisplay.SetSelectedCell(position, _currentMoves.Length > 0);
        }
        else
        {
            IntermediateDisplay.ResetSelectedCell();
        }
    }

    private void DisplayPartialMove()
    {
        IntermediateDisplay.SetDisplayedAvailableMoves(
            _partialMoves!.Select(move => new MoveDisplayInfo
            {
                PathColor = Color.Green,
                FullInfo = move
            }).ToArray());
        IntermediateDisplay.SetPartialMovePreview(_partialPathIndex);
    }

    private void ResetMoves()
    {
        _currentMoves = null;
        _partialMoves = null;

        IntermediateDisplay.ResetDisplayedMoves();
        IntermediateDisplay.ResetPartialMove();
        IntermediateDisplay.ResetSelectedCell();
    }

    private void UpdateAvailableMoves(Position position)
    {
        var piece = Board.GetPieceAt(position);
        if (piece.IsEmpty)
        {
            ResetMoves();
            return;
        }

        _currentMoves = Board.MoveGenerator.GenerateMovesForPiece(new PieceOnBoard(position, piece))
            .Select(move => Board.MoveGenerator.GetMoveFullInfo(move))
            .ToArray();

        _partialMoves = null;
        UpdateDisplayedMoves();
    }
}