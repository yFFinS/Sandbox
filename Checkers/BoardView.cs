using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Checkers;

public class BoardView
{
    private readonly Stack<BoardState> _gameHistory = new();

    private readonly GraphicsDevice _device;
    public readonly Board Board;
    private readonly CheckersAi _ai;
    public int CellSize { get; private set; }

    private readonly BoardDrawer _boardDrawer;
    private bool _helpEnabled;

    public BoardView(GraphicsDevice device, Board board)
    {
        _device = device;
        Board = board;

        _ai = new CheckersAi(Board);
        _ai.Configure(config =>
        {
            config.MaxSearchDepth = 5;
            config.UseMultithreading = true;
        });

        _boardDrawer = new BoardDrawer(device);
        _boardDrawer.SetTargetBoard(Board);

        UpdateCellSize();
    }

    private void UpdateCellSize()
    {
        var width = _device.Viewport.Width / Board.Size;
        var height = _device.Viewport.Height / Board.Size;
        CellSize = Math.Min(width, height);
        _boardDrawer.SetCellSize(CellSize);
    }

    public void Update(GameTime gameTime)
    {
        if (Input.IsKeyDown(Keys.Left))
        {
            if (_gameHistory.TryPop(out var state))
            {
                ResetMoves();
                Board.SetState(state);
            }

            return;
        }

        if (Input.IsKeyDown(Keys.Escape))
        {
            Board.Reset();
            return;
        }

        if (Input.IsKeyDown(Keys.H))
        {
            _helpEnabled = !_helpEnabled;
            if (!_helpEnabled)
            {
                _hintMove = null;
                _boardDrawer.SetHintMove(null);
            }
        }

        if (_helpEnabled && _hintMove is null)
        {
            _hintMove = _ai.GetNextMove();
            _boardDrawer.SetHintMove(_hintMove);
        }

        if (!Input.IsButtonDown(0))
        {
            return;
        }

        if (_gameEnded)
        {
            Board.Reset();
            _gameEnded = false;
            return;
        }
        
        var mousePosition = Input.MousePosition;
        var cellPosition = new Position(mousePosition.X / CellSize, mousePosition.Y / CellSize);
        if (!Board.IsInBounds(cellPosition))
        {
            return;
        }

        HandleCellClick(cellPosition);
    }

    private void ResetMoves()
    {
        _currentMoves = null;
        _boardDrawer.SetAvailableMoves(null);
        _boardDrawer.SetCollidingMoves(null);
        _traversedPartialPath = 0;
        _partialMoves = null;
        _hintMove = null;
        _boardDrawer.SetHintMove(null);
    }

    private void HandleCellClick(Position position)
    {
        bool TryMakeMove()
        {
            var moves = _partialMoves ?? _currentMoves;
            var collisions = moves.Where(move => move.Move.Path.Contains(position)).ToArray();

            switch (collisions.Length)
            {
                case 0:
                    UpdateAvailableMoves();
                    break;
                case 1:
                    var move = collisions[0];
                    if (move.Move.Path[^1] == position)
                    {
                        ResetMoves();
                        _gameHistory.Push(Board.GetState());
                        
                        Board.MakeMove(move.Move);
                        return true;
                    }

                    MakePartialMove(new[] { move }, position);
                    break;
                default:
                    var partialMoves = collisions
                        .Where(m => m.Move.Path[_traversedPartialPath] == position)
                        .ToArray();
                    if (partialMoves.Length > 0)
                    {
                        MakePartialMove(partialMoves, position);
                    }
                    else
                    {
                        _boardDrawer.SetCollidingMoves(collisions.Select(c => c.Move).ToArray());
                    }

                    break;
            }

            return false;
        }

        void UpdateAvailableMoves()
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
            _boardDrawer.SetAvailableMoves(_currentMoves);
        }

        _boardDrawer.SetClickPosition(position);

        var moveCompleted = false;
        if (_currentMoves is null)
        {
            UpdateAvailableMoves();
        }
        else
        {
            moveCompleted = TryMakeMove();
        }

        if (!moveCompleted)
        {
            return;
        }

        _gameEnded = Board.GetGameEndState() != GameEndState.None;
        if (!_gameEnded)
        {
            var startTime = Stopwatch.GetTimestamp();
            Board.MakeMove(_ai.GetNextMove());
            var tookTimeInTicks = Stopwatch.GetTimestamp() - startTime;
            var time = (float)tookTimeInTicks / Stopwatch.Frequency;
            Console.WriteLine($"Ai made move in {time * 1000:F0} ms");
        }

        _gameEnded = Board.GetGameEndState() != GameEndState.None;
    }

    private bool _gameEnded;

    private void MakePartialMove(MoveFullInfo[] possibleMoves, Position clickedPosition)
    {
        _partialMoves = possibleMoves;
        _traversedPartialPath = Array.IndexOf(_partialMoves[0].Move.Path.ToArray(), clickedPosition) + 1;
        _boardDrawer.SetAvailableMoves(_partialMoves, _traversedPartialPath);
    }

    private MoveFullInfo[]? _partialMoves;
    private int _traversedPartialPath;
    private IReadOnlyList<MoveFullInfo>? _currentMoves;
    private Move? _hintMove;

    public void Draw(GameTime gameTime)
    {
        _boardDrawer.Draw(gameTime);
    }
}