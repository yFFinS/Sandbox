using Chess.Core;
using Microsoft.Xna.Framework;

namespace Chess.View;

public class MoveAnimator
{
    public float Speed { get; set; } = 2f;

    private readonly ChessBoard _board;
    private readonly BoardDrawable _boardDrawable;

    public MoveAnimator(ChessBoard board, BoardDrawable boardDrawable)
    {
        _board = board;
        _boardDrawable = boardDrawable;
    }

    private Move? _move;
    private PieceDrawable? _piece;
    private PieceDrawable? _rook;
    private float _animationFrameTime;

    public void ConfirmEnding()
    {
        if (_move is null || _piece is null)
        {
            throw new NullReferenceException();
        }

        var move = _move.Value;
        _piece.BoardPosition = move.End;
        _piece.DrawOrder = 0;

        switch (move.Type)
        {
            case MoveType.Quiet:
            case MoveType.Capture:
            case MoveType.DoublePawn:
            case MoveType.EnPassant:
                break;
            case MoveType.KingsideCastle:
            case MoveType.QueensideCastle:
                var color = _board.IsOfColorAt(PieceColor.Black, _move!.Value.Start) ? PieceColor.Black : PieceColor.White;
                var rookEndPos = _move!.Value.Type == MoveType.KingsideCastle
                    ? _board.GetKingsideCastleRookEnd(color)
                    : _board.GetQueensideCastleRookEnd(color);
                _rook!.BoardPosition = rookEndPos;
                break;
            case MoveType.KnightPromotion:
                _piece.Promote(PieceType.Knight);
                break;
            case MoveType.BishopPromotion:
                _piece.Promote(PieceType.Bishop);
                break;
            case MoveType.RookPromotion:
                _piece.Promote(PieceType.Rook);
                break;
            case MoveType.QueenPromotion:
                _piece.Promote(PieceType.Queen);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        _move = null;
        _piece = null;
        _rook = null;

        WaitingForEndConfirm = false;
    }

    public void AnimateMove(Move move)
    {
        _move = move;
        _piece = _boardDrawable.GetPieceAt(move.Start);
        _animationFrameTime = 0;
        _piece!.DrawOrder = 2;

        if (move.Type is not (MoveType.KingsideCastle or MoveType.QueensideCastle))
        {
            return;
        }

        var color = _board.IsOfColorAt(PieceColor.Black, _move!.Value.Start) ? PieceColor.Black : PieceColor.White;
        var rookStartPos = _move!.Value.Type == MoveType.KingsideCastle
            ? _board.GetKingsideCastleRookStart(color)
            : _board.GetQueensideCastleRookStart(color);
        _rook = _boardDrawable.GetPieceAt(rookStartPos);
        _rook!.DrawOrder = 1;
    }

    public void Update(GameTime gameTime)
    {
        if (_move is null || WaitingForEndConfirm)
        {
            return;
        }

        _animationFrameTime += gameTime.DeltaTime() * Speed;
        var animationEnded = false;
        if (_animationFrameTime >= 1)
        {
            animationEnded = true;
            _animationFrameTime = 1;
        }

        MoveAnimatingPiece();
        MoveRookIfCastle();

        if (!animationEnded)
        {
            return;
        }

        var endPiece = _boardDrawable.GetPieceAt(_move.Value.End);
        if (endPiece is not null)
        {
            _boardDrawable.RemovePiece(endPiece);
        }

        if (_move.Value.Type == MoveType.EnPassant)
        {
            var capturedPawn = _boardDrawable.Board.GetEnPassantCapturedPawn(_move.Value);
            _boardDrawable.RemovePieceAt(capturedPawn);
        }

        WaitingForEndConfirm = true;
    }

    private void MoveRookIfCastle()
    {
        if (_rook is null)
        {
            return;
        }

        var color = _board.IsOfColorAt(PieceColor.Black, _move!.Value.Start) ? PieceColor.Black : PieceColor.White;
        var rookStartPos = _move!.Value.Type == MoveType.KingsideCastle
            ? _board.GetKingsideCastleRookStart(color)
            : _board.GetQueensideCastleRookStart(color);
        var rookEndPos = _move!.Value.Type == MoveType.KingsideCastle
            ? _board.GetKingsideCastleRookEnd(color)
            : _board.GetQueensideCastleRookEnd(color);
        var rookStart = _boardDrawable.ToScreenPosition(rookStartPos);
        var rookEnd = _boardDrawable.ToScreenPosition(rookEndPos);
        var currentRookPosition = Vector2.Lerp(rookStart, rookEnd, _animationFrameTime);

        _rook.ScreenPosition = currentRookPosition;
    }

    private void MoveAnimatingPiece()
    {
        var start = _boardDrawable.ToScreenPosition(_move!.Value.Start);
        var target = _boardDrawable.ToScreenPosition(_move.Value.End);
        var currentPosition = Vector2.Lerp(start, target, _animationFrameTime);

        _piece!.ScreenPosition = currentPosition;
    }

    public bool WaitingForEndConfirm { get; private set; }
}