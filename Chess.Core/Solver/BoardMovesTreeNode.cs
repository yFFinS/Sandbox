namespace Chess.Core.Solver;

public class BoardMovesTreeNode
{
    public bool IsRoot => Parent is null;
    public BoardMovesTreeNode? Parent { get; init; }

    public readonly List<BoardMovesTreeNode> Children = new();
    public Move Move { get; init; }
    public bool IsExpanded { get; set; }
    public int Score { get; set; }
}