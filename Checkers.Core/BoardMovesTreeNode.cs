namespace Checkers.Core;

public class BoardMovesTreeNode
{
    public bool IsRoot => Parent is null;
    public BoardMovesTreeNode? Parent { get; init; }

    public readonly List<BoardMovesTreeNode> Children = new();

    public Board? Board { get; init; }
    public Move? LeadingMove { get; init; }
    public bool IsExpanded { get; set; }
    public int Score { get; set; }
}