namespace Chess.Core;

public readonly struct ReadOnlyPieceIndexer<TValue>
{
    private readonly ByPieceIndexer<TValue> _indexer;

    public ReadOnlyPieceIndexer(ByPieceIndexer<TValue> indexer)
    {
        _indexer = indexer;
    }

    public static implicit operator ReadOnlyPieceIndexer<TValue>(ByPieceIndexer<TValue> indexer)
    {
        return new ReadOnlyPieceIndexer<TValue>(indexer);
    }

    public TValue Get(PieceType pieceType)
    {
        return _indexer.Get(pieceType);
    }
}