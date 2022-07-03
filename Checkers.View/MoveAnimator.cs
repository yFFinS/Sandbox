using Checkers.Core;
using Microsoft.Xna.Framework;

namespace Checkers.View;

internal class MoveAnimator
{
    private readonly BoardDrawable _boardDrawable;

    private MoveInfo? _animatingMove;
    private PieceDrawable? _animatingPiece;
    private AnimatorState _animatorState;

    private float _waitTime;
    private float _animationFrameTime;
    private int _currentPathIndex = -1;
    private float _waitTimePerStep = 0.15f;
    private float _animationSpeed = 4f;

    public float WaitTimePerStep
    {
        get => _waitTimePerStep;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("Value must be positive.", nameof(value));
            }

            _waitTimePerStep = value;
        }
    }

    public float AnimationSpeed
    {
        get => _animationSpeed;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentException("Value must be positive.", nameof(value));
            }

            _animationSpeed = value;
        }
    }

    public bool WaitingForEndingConfirm => _animatorState == AnimatorState.AnimationEnded;
    public bool Animating => _animatorState is AnimatorState.PlayingAnimation or AnimatorState.Waiting;

    public void ConfirmEnding()
    {
        _animatingPiece!.BoardPosition = _animatingMove!.EndPosition;
        _animatorState = AnimatorState.Idle;
        _removedPieces.Clear();

        _animatingPiece!.DrawOrder = 0;
        _animatingMove = null;
        _animatingPiece = null;
        _currentPathIndex = -1;
    }

    private enum AnimatorState
    {
        Idle = 0,
        Waiting,
        PlayingAnimation,
        AnimationEnded
    }

    public MoveAnimator(BoardDrawable boardDrawable)
    {
        _boardDrawable = boardDrawable;
    }

    public void AnimateMove(MoveInfo moveInfo, bool continueFromCurrentState = false)
    {
        AnimateMoveToIndex(moveInfo, moveInfo.Move.Path.Count - 1, continueFromCurrentState);
    }

    public void ResetCurrentAnimation()
    {
        if (_animatingMove is null)
        {
            return;
        }

        foreach (var removedPiece in _removedPieces.Values)
        {
            _boardDrawable.AddPiece(removedPiece);
        }

        _removedPieces.Clear();
        _animatingPiece!.Position = _boardDrawable.ToScreenPosition(_animatingMove.StartPosition);
        _boardDrawable.CellsController.ResetUpdatedPathCells();

        _animatingPiece = null;
        _animatingMove = null;
        _currentPathIndex = -1;
        _animatorState = AnimatorState.Idle;
    }

    private int _endIndex;
    private readonly Dictionary<int, PieceDrawable> _removedPieces = new();

    public void AnimateMoveToIndex(MoveInfo moveInfo, int pathIndex, bool continueFromCurrentState = false)
    {
        _animatingMove = moveInfo;
        _endIndex = pathIndex;
        _animatorState = AnimatorState.PlayingAnimation;

        if (continueFromCurrentState)
        {
            _isAnimatingBackwards = _endIndex < _currentPathIndex;
            return;
        }

        _isAnimatingBackwards = false;
        _boardDrawable.CellsController.ResetUpdatedPathCells();

        _animatingPiece = _boardDrawable.GetPieceAt(_animatingMove.StartPosition)!;
        _animatingPiece.DrawOrder = 1;

        var startCell = _boardDrawable.GetCellAt(_animatingMove.StartPosition)!;
        _boardDrawable.CellsController.MarkCell(startCell, CellMarker.MoveStart);
    }

    public void Update(GameTime gameTime)
    {
        switch (_animatorState)
        {
            case AnimatorState.Idle or AnimatorState.AnimationEnded:
                return;
            case AnimatorState.Waiting:
                if (_waitTime < WaitTimePerStep)
                {
                    _waitTime += gameTime.DeltaTime();
                    return;
                }

                ContinueAnimating();
                break;
            case AnimatorState.PlayingAnimation:
                _animationFrameTime += gameTime.DeltaTime() * AnimationSpeed;

                var targetPosition = GetTargetPosition();
                var targetScreenPosition = _boardDrawable.ToScreenPosition(targetPosition);

                if (_animationFrameTime >= 1)
                {
                    ProcessAnimationFramePassed(targetScreenPosition);

                    if (_currentPathIndex == _endIndex)
                    {
                        _animatorState = AnimatorState.AnimationEnded;
                        return;
                    }

                    StartWaiting();
                    return;
                }

                var startPosition = GetStartPosition();
                var startScreenPosition = _boardDrawable.ToScreenPosition(startPosition);
                MoveAnimatingPieceSmoothly(startScreenPosition, targetScreenPosition);

                break;
        }
    }

    private void ContinueAnimating()
    {
        _waitTime = 0;
        _animatorState = AnimatorState.PlayingAnimation;
    }

    private void MoveAnimatingPieceSmoothly(Vector2 from, Vector2 to)
    {
        _animatingPiece!.Position = Vector2.Lerp(from, to, _animationFrameTime);
    }

    private void StartWaiting()
    {
        _animatorState = AnimatorState.Waiting;
        _waitTime = 0;
    }

    private void ProcessAnimationFramePassed(Vector2 targetScreenPosition)
    {
        _animationFrameTime = 0;
        _currentPathIndex += _isAnimatingBackwards ? -1 : 1;

        _animatingPiece!.Position = targetScreenPosition;

        var pathCell = _boardDrawable.GetCellAt(_animatingMove!.Move.Path[_currentPathIndex])!;
        _boardDrawable.CellsController.MarkCell(pathCell, CellMarker.MovePath);

        var capturedPositions = _animatingMove!.CapturedPositions;
        if (_currentPathIndex < capturedPositions.Count)
        {
            var removingPiece = _boardDrawable.GetPieceAt(capturedPositions[_currentPathIndex])!;
            _removedPieces[_currentPathIndex] = removingPiece;
            _boardDrawable.RemovePiece(removingPiece);
        }

        if (_animatingMove.HasPromoted && _animatingMove.PromotionPathIndex == _currentPathIndex)
        {
            _animatingPiece.Promote();
        }
    }

    private Position GetStartPosition()
    {
        return _currentPathIndex == -1
            ? _animatingMove!.StartPosition
            : _animatingMove!.Move.Path[_currentPathIndex];
    }

    private Position GetTargetPosition()
    {
        var targetIndex = _currentPathIndex + (_isAnimatingBackwards ? -1 : 1);
        return targetIndex == -1 ? _animatingMove!.StartPosition : _animatingMove!.Move.Path[targetIndex];
    }

    private bool _isAnimatingBackwards;
}