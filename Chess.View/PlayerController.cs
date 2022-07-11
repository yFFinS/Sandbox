using Assimp;
using Checkers.View;
using Chess.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Sandbox.Shared;

namespace Chess.View;

public class PlayerController : AbstractBoardController
{
    private enum PlayerMode
    {
        Playing = 0,
        Editing
    }

    private PlayerMode _playerMode;
    private bool _gameEnded;
    private PieceColor _myColor;

    public override void OnTurnBegan()
    {
        _gameEnded = Board.IsGameEnded();
        UpdateMustCaptureCellsIfAny();
    }

    private void UpdateMustCaptureCellsIfAny()
    {
        var hasCaptures = false;
        // foreach (var move in Board.MoveGenerator.GenerateAllMoves())
        // {
        //     if (!hasCaptures)
        //     {
        //         var isCapturing = Board.MoveGenerator.GetAllMoveCaptures(move).Any();
        //         if (!isCapturing)
        //         {
        //             break;
        //         }
        //
        //         hasCaptures = true;
        //     }
        //
        //     var startCell = Drawable.GetCellAt(move.StartPosition)!;
        //     Drawable.CellsController.MarkCell(startCell, CellMarker.MustCapture);
        // }
    }

    protected override void OnGameStarted(PlayerType opponentType)
    {
        _opponentType = opponentType;
        _myColor = IsMyTurn ? Board.ColorToMove :
            Board.ColorToMove == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        ResetMoves();
    }

    public override void OnTurnEnded()
    {
        _gameEnded = Board.IsGameEnded();
    }

    public override void OnInitialized()
    {
        _moveAnimator = new MoveAnimator(Board, Drawable);
    }

    public override void Update(GameTime gameTime, ControllerVisitor visitor)
    {
        _moveAnimator.Update(gameTime);

        if (Input.IsKeyDown(Keys.B))
        {
            Drawable.InitializeFromBoard(Board);
        }

        if (_isWaitingForAnimatorToFinish)
        {
            if (_moveAnimator.WaitingForEndConfirm)
            {
                EndAnimationAndMakeMove(_finalMove, visitor);
            }

            return;
        }

        if (Input.IsKeyDown(Keys.M))
        {
            SwitchPlayerMode();
            return;
        }

        if (Input.IsButtonDown(MouseButton.Left) && _gameEnded)
        {
            RestartGame(visitor);
            return;
        }

        if (Input.IsButtonDown(MouseButton.Left) && _playerMode == PlayerMode.Playing)
        {
            var cellPosition = Drawable.ToBoardPosition(Input.MousePosition);
            HandleCellClick(cellPosition, visitor);
            return;
        }

        if (!IsMyTurn)
        {
            return;
        }

        if (_playerMode == PlayerMode.Editing)
        {
            HandleEditing();
            return;
        }

        if (Input.IsKeyDown(Keys.Escape))
        {
            visitor.OpenPauseMenu();
        }
        else if (Input.IsButtonDown(MouseButton.Right))
        {
            ResetMoves();
        }
    }

    private void SwitchPlayerMode()
    {
        _playerMode = _playerMode == PlayerMode.Playing ? PlayerMode.Editing : PlayerMode.Playing;
    }

    private void HandleEditing()
    {
        var leftClick = Input.IsButtonDown(MouseButton.Left);
        var rightClick = Input.IsButtonDown(MouseButton.Right);

        if (!leftClick && !rightClick)
        {
            return;
        }

        var cell = Drawable.ToBoardPosition(Input.MousePosition);
        if (cell is < 0 or >= 64)
        {
            return;
        }

        var color = leftClick ? PieceColor.White : PieceColor.Black;
        var piece = Board.GetPieceAt(cell);

        if (piece.IsEmpty)
        {
            Board.SetPieceAt(cell, new Piece(color, PieceType.Pawn));
        }
        else if (piece.Type == PieceType.Queen || piece.Color != color)
        {
            Board.SetPieceAt(cell, Piece.Empty);
        }
        else
        {
            Board.SetPieceAt(cell, new Piece(color, PieceType.Queen));
        }

        Drawable.InitializeFromBoard(Board);
    }

    private void EndAnimationAndMakeMove(Move finalMove, ControllerVisitor visitor)
    {
        _moveAnimator.ConfirmEnding();
        _isWaitingForAnimatorToFinish = false;
        visitor.MakeMove(finalMove);
        visitor.PassTurn();
    }

    private void RestartGame(ControllerVisitor visitor)
    {
        Board.ResetToDefaultPosition();
        ResetMoves();

        _playerMode = PlayerMode.Playing;
        _gameEnded = false;
        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.CellsController.ResetUpdatedPathCells();
        Drawable.InitializeFromBoard(Board);

        visitor.RestartGame();
    }

    private void SetDisplayedMoves(IEnumerable<Move>? moves)
    {
        Drawable.ClearMoves();

        if (moves is null)
        {
            return;
        }

        foreach (var move in moves)
        {
            var moveDrawable = new MoveDrawable(move);
            Drawable.AddMove(moveDrawable);
        }
    }

    private void MakeMoveWithoutAnimations(MoveInfo moveInfo, ControllerVisitor visitor)
    {
        ResetMoves();

        // Drawable.CellsController.ResetUpdatedMustCaptureCells();
        // _gameHistory.Push(Board.GetState());
        //
        // UpdateDrawableCellsFromMove(moveInfo);
        // UpdateDrawablePiecesFromMove(moveInfo);
        //
        // visitor.MakeMove(moveInfo.Move);
        // visitor.PassTurn();
    }

    private void UpdateDrawablePiecesFromMove(MoveInfo moveInfo)
    {
        // foreach (var capturedPosition in moveInfo.CapturedPositions)
        // {
        //     Drawable.RemovePieceAt(capturedPosition);
        // }
        //
        // var movedPiece = Drawable.GetPieceAt(moveInfo.StartPosition)!;
        // movedPiece.BoardPosition = moveInfo.EndPosition;
        // movedPiece.Position = Drawable.ToScreenPosition(moveInfo.EndPosition);
        //
        // if (moveInfo.HasPromoted)
        // {
        //     movedPiece.Promote();
        // }
    }

    private void UpdateDrawableCellsFromMove(MoveInfo moveInfo)
    {
        // Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        // Drawable.CellsController.ResetUpdatedPathCells();
        //
        // var startCell = Drawable.GetCellAt(moveInfo.StartPosition)!;
        // Drawable.CellsController.MarkCell(startCell, CellMarker.MoveStart);
        //
        // var pathCells = moveInfo.Move.Path.Select(pos => Drawable.GetCellAt(pos));
        // foreach (var pathCell in pathCells)
        // {
        //     Drawable.CellsController.MarkCell(pathCell!, CellMarker.MovePath);
        // }
    }

    private bool TryMovePieceToPosition(IEnumerable<Move> moves, int position, ControllerVisitor visitor,
        bool withAnimations)
    {
        var selectedMove = moves.FirstOrDefault(move => move.End == position);
        if (selectedMove.IsEmpty)
        {
            return false;
        }

        ResetMoves();
        _isWaitingForAnimatorToFinish = true;

        _finalMove = selectedMove;
        _moveAnimator.AnimateMove(_finalMove);
        return true;
    }

    private void MakeFinalMoveOrScheduleAnimation(MoveInfo moveInfo, ControllerVisitor visitor, bool withAnimations)
    {
        // Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        // Drawable.ClearMoves();
        //
        // if (withAnimations)
        // {
        //     _finalMove = moveInfo;
        //     _moveAnimator.AnimateMove(moveInfo, _isMidPartialMove);
        //     _isWaitingForAnimatorToFinish = true;
        // }
        // else
        // {
        //     MakeMoveWithoutAnimations(moveInfo, visitor);
        // }
    }

    private bool _isWaitingForAnimatorToFinish;
    private PlayerType _opponentType;
    private Move[]? _moves;
    private Move _finalMove;
    private MoveAnimator _moveAnimator = null!;

    private void HandleCellClick(int position, ControllerVisitor visitor)
    {
        if (!IsMyTurn && _opponentType == PlayerType.Local)
        {
            return;
        }

        if (position is < 0 or >= 64)
        {
            //_lastSelectedPiecePosition = null;
            return;
        }

        var piece = Drawable.GetPieceAt(position);

        var moved = false;
        if (_moves is not null)
        {
            moved = TryMovePieceToPosition(_moves, position, visitor, true);
        }

        if (!moved)
        {
            UpdateAvailableMoves(position);
        }
    }

    private void ResetMoves()
    {
        _moves = null;
        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.ClearMoves();
    }

    private void UpdateAvailableMoves(int position)
    {
        var piece = Board.GetPieceAt(position);
        if (piece.IsEmpty)
        {
            ResetMoves();
            return;
        }

        if (IsMyTurn)
        {
            _moves = Board.MoveGenerator.GetMovesFrom(position).ToArray();
            SetDisplayedMoves(_moves);
        }
    }
}