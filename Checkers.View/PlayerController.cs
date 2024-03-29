﻿using System.Diagnostics.CodeAnalysis;
using Checkers.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Sandbox.Shared;

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
    private MoveInfo[]? _currentMoves;
    private MoveInfo[]? _partialMoves;
    private MoveAnimator _moveAnimator = null!;
    private int _partialPathIndex = -1;
    private Position? _lastSelectedPiecePosition;
    private PieceColor _myColor;

    public override void OnTurnBegan(MoveInfo? opponentMoveInfo)
    {
        _gameEnded = Board.IsGameEnded();
        UpdateMustCaptureCellsIfAny();

        if (_lastSelectedPiecePosition.HasValue)
        {
            UpdateAvailableMoves(_lastSelectedPiecePosition.Value);
            _lastSelectedPiecePosition = null;
        }
    }

    private void UpdateMustCaptureCellsIfAny()
    {
        var hasCaptures = false;
        foreach (var move in Board.MoveGenerator.GenerateAllMoves())
        {
            if (!hasCaptures)
            {
                var isCapturing = Board.MoveGenerator.GetAllMoveCaptures(move).Any();
                if (!isCapturing)
                {
                    break;
                }

                hasCaptures = true;
            }

            var startCell = Drawable.GetCellAt(move.StartPosition)!;
            Drawable.CellsController.MarkCell(startCell, CellMarker.MustCapture);
        }
    }

    protected override void OnGameStarted(PlayerType opponentType)
    {
        _opponentType = opponentType;
        _myColor = IsMyTurn ? Board.CurrentTurn :
            Board.CurrentTurn == PieceColor.Black ? PieceColor.White : PieceColor.Black;
        ResetMoves();
    }

    public override void OnTurnEnded()
    {
        _gameEnded = Board.IsGameEnded();
    }

    public override void OnInitialized()
    {
        _moveAnimator = new MoveAnimator(Drawable);
    }

    private MoveInfo? _finalMove;

    public override void Update(GameTime gameTime, ControllerVisitor visitor)
    {
        _moveAnimator.Update(gameTime);

        if (_isWaitingForAnimatorToFinish)
        {
            if (_moveAnimator.WaitingForEndingConfirm)
            {
                EndAnimationAndMakeMove(_finalMove!, visitor);
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

        if (Input.IsKeyDown(Keys.Left))
        {
            TryRollbackGameState();
        }
        else if (Input.IsKeyDown(Keys.Escape))
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

        Drawable.InitializeFromBoard(Board);
    }

    private void EndAnimationAndMakeMove(MoveInfo finalMove, ControllerVisitor visitor)
    {
        _moveAnimator.ConfirmEnding();
        _isWaitingForAnimatorToFinish = false;
        _isMidPartialMove = false;

        Drawable.CellsController.ResetUpdatedMustCaptureCells();
        visitor.MakeMove(finalMove.Move);
        visitor.PassTurn();
    }

    private void RestartGame(ControllerVisitor visitor)
    {
        Board.Reset();
        ResetMoves();

        _playerMode = PlayerMode.Playing;
        _gameEnded = false;
        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.CellsController.ResetUpdatedPathCells();
        Drawable.InitializeFromBoard(Board);

        visitor.RestartGame();
    }

    private void TryRollbackGameState()
    {
        return;

        // TODO: not implemented
        if (_gameHistory.Count == 0)
        {
            return;
        }

        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.CellsController.ResetUpdatedPathCells();

        Board.SetState(_gameHistory.Pop());
        ResetMoves();

        Drawable.InitializeFromBoard(Board);
    }

    private void UpdateDisplayedMoves()
    {
        Drawable.ClearMoves();

        if (_currentMoves is null)
        {
            return;
        }

        foreach (var availableMove in _currentMoves)
        {
            var moveDrawable = new MoveDrawable(availableMove);
            Drawable.AddMove(moveDrawable);
        }
    }

    private void MakeMoveWithoutAnimations(MoveInfo moveInfo, ControllerVisitor visitor)
    {
        ResetMoves();

        Drawable.CellsController.ResetUpdatedMustCaptureCells();
        _gameHistory.Push(Board.GetState());

        UpdateDrawableCellsFromMove(moveInfo);
        UpdateDrawablePiecesFromMove(moveInfo);

        visitor.MakeMove(moveInfo.Move);
        visitor.PassTurn();
    }

    private void UpdateDrawablePiecesFromMove(MoveInfo moveInfo)
    {
        foreach (var capturedPosition in moveInfo.CapturedPositions)
        {
            Drawable.RemovePieceAt(capturedPosition);
        }

        var movedPiece = Drawable.GetPieceAt(moveInfo.StartPosition)!;
        movedPiece.BoardPosition = moveInfo.EndPosition;
        movedPiece.Position = Drawable.ToScreenPosition(moveInfo.EndPosition);

        if (moveInfo.HasPromoted)
        {
            movedPiece.Promote();
        }
    }

    private void UpdateDrawableCellsFromMove(MoveInfo moveInfo)
    {
        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.CellsController.ResetUpdatedPathCells();

        var startCell = Drawable.GetCellAt(moveInfo.StartPosition)!;
        Drawable.CellsController.MarkCell(startCell, CellMarker.MoveStart);

        var pathCells = moveInfo.Move.Path.Select(pos => Drawable.GetCellAt(pos));
        foreach (var pathCell in pathCells)
        {
            Drawable.CellsController.MarkCell(pathCell!, CellMarker.MovePath);
        }
    }

    private void TryMovePieceToPosition(Position position, ControllerVisitor visitor, bool withAnimations)
    {
        var moves = _partialMoves ?? _currentMoves;
        var collisions = moves!.Where(move => move.Move.Path.Contains(position)).ToArray();

        switch (collisions.Length)
        {
            case 0:
                UpdateAvailableMoves(position);
                return;
            case 1:
                var move = collisions[0];
                if (move.Move.Path[^1] == position)
                {
                    MakeFinalMoveOrScheduleAnimation(move, visitor, withAnimations);
                    return;
                }

                if (move.Move.Path.IndexOf(position, _partialPathIndex + 1) == _partialPathIndex + 1)
                {
                    ContinuePartialMove(collisions, move);
                    DisplayPartialMove();
                }
                else
                {
                    UpdateAvailableMoves(position);
                }

                break;
            default:
                /*
                 * Пытаемся найти и выполнить ход, в котором выбранная клетка является следующей по пути.
                 * Если такого нет, то возвращаем фигуру на её начальное место и сбрасываем анимацию.
                */

                var oneStepMove =
                    collisions.FirstOrDefault(col =>
                        col.Move.Path.IndexOf(position, _partialPathIndex + 1) == _partialPathIndex + 1);

                if (oneStepMove is null)
                {
                    UpdateAvailableMoves(position);
                    return;
                }

                if (oneStepMove.Move.Path[^1] == position)
                {
                    MakeFinalMoveOrScheduleAnimation(oneStepMove, visitor, withAnimations);
                }

                ContinuePartialMove(collisions, oneStepMove);
                DisplayPartialMove();
                break;
        }
    }

    private void MakeFinalMoveOrScheduleAnimation(MoveInfo moveInfo, ControllerVisitor visitor, bool withAnimations)
    {
        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.ClearMoves();

        if (withAnimations)
        {
            _finalMove = moveInfo;
            _moveAnimator.AnimateMove(moveInfo, _isMidPartialMove);
            _isWaitingForAnimatorToFinish = true;
        }
        else
        {
            MakeMoveWithoutAnimations(moveInfo, visitor);
        }
    }

    private bool _isWaitingForAnimatorToFinish;
    private bool _isMidPartialMove;
    private PlayerType _opponentType;

    [MemberNotNull(nameof(_partialMoves))]
    private void ContinuePartialMove(IEnumerable<MoveInfo> collisions, MoveInfo selectedMove)
    {
        _isMidPartialMove = true;
        _partialPathIndex++;
        var position = selectedMove.Move.Path[_partialPathIndex];
        _partialMoves = collisions
            .Where(m => m.Move.Path[_partialPathIndex] == position)
            .ToArray();
    }

    private void HandleCellClick(Position position, ControllerVisitor visitor)
    {
        if (!IsMyTurn && _opponentType == PlayerType.Local)
        {
            return;
        }

        if (!Board.IsInBounds(position))
        {
            _lastSelectedPiecePosition = null;
            return;
        }

        var piece = Drawable.GetPieceAt(position);
        if (piece is not null)
        {
            UpdateAvailableMoves(position);
            return;
        }

        if (_currentMoves is not null)
        {
            TryMovePieceToPosition(position, visitor, true);
        }
    }

    private void DisplayPartialMove()
    {
        var continueFromCurrentState = _partialPathIndex > 0;
        var moveInfo = _partialMoves![0];
        _moveAnimator.AnimateMoveToIndex(moveInfo, _partialPathIndex, continueFromCurrentState);

        Drawable.ClearMoves();

        foreach (var partialMove in _partialMoves)
        {
            var moveDrawable = new MoveDrawable(partialMove)
            {
                StaringIndex = _partialPathIndex
            };

            Drawable.AddMove(moveDrawable);
        }
    }

    private void ResetMoves()
    {
        _currentMoves = null;
        _partialMoves = null;
        _partialPathIndex = -1;
        _isMidPartialMove = false;

        Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
        Drawable.ClearMoves();
    }

    private void UpdateAvailableMoves(Position position)
    {
        _moveAnimator.ResetCurrentAnimation();

        var piece = Board.GetPieceAt(position);
        if (piece.IsEmpty)
        {
            _lastSelectedPiecePosition = null;
            ResetMoves();
            return;
        }

        var moveAvailable = false;
        var selectedMyPiece = piece.Color == _myColor;
        if (IsMyTurn)
        {
            _currentMoves = Board.MoveGenerator.GenerateMovesForPiece(new PieceOnBoard(position, piece))
                .Select(move => Board.MoveGenerator.GetMoveInfo(move))
                .ToArray();
            moveAvailable = _currentMoves.Length > 0;
        }
        else if (_opponentType != PlayerType.Local)
        {
            _lastSelectedPiecePosition = position;
        }

        if (selectedMyPiece)
        {
            Drawable.CellsController.ResetUpdatedMoveIndicatorCells();
            var cell = Drawable.GetCellAt(position)!;
            var marker = moveAvailable ? CellMarker.MoveAvailable : CellMarker.NoMoveAvailable;

            Drawable.CellsController.MarkCell(cell, marker);
        }

        _partialMoves = null;
        _partialPathIndex = -1;

        if (IsMyTurn)
        {
            UpdateDisplayedMoves();
        }
    }
}