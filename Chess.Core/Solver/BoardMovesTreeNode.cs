namespace Chess.Core.Solver;

public class BoardMovesTreeNode
{
    public bool IsRoot => Parent is null;
    public BoardMovesTreeNode? Parent { get; init; }

    public readonly List<BoardMovesTreeNode> Children = new();

    public ChessBoard? Board { get; init; }
    public Move? LeadingMove { get; init; }
    public bool IsExpanded { get; set; }
    public double Score { get; set; }
}