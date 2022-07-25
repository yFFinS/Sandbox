namespace Chess.Core;

public readonly struct ReadOnlyColorIndexer<TValue>
{
    private readonly ByColorIndexer<TValue> _indexer;

    public ReadOnlyColorIndexer(ByColorIndexer<TValue> indexer)
    {
        _indexer = indexer;
    }

    public static implicit operator ReadOnlyColorIndexer<TValue>(ByColorIndexer<TValue> indexer)
    {
        return new ReadOnlyColorIndexer<TValue>(indexer);
    }

    public TValue Get(PieceColor color)
    {
        return _indexer.Get(color);
    }
}