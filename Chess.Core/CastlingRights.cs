namespace Chess.Core;

public struct CastlingRights
{
    private int _value;

    public bool CanCastle(PieceColor color, CastleType castleType)
    {
        var castleKey = GetCastleKey(color, castleType);
        return (_value & castleKey) != 0;
    }

    public void SetCastleAllowed(PieceColor color, CastleType castleType, bool allowed)
    {
        if (allowed)
        {
            AllowCastle(color, castleType);
        }
        else
        {
            DisallowCastle(color, castleType);
        }
    }

    public void AllowCastle(PieceColor color, CastleType castleType)
    {
        var castleKey = GetCastleKey(color, castleType);
        _value |= castleKey;
    }

    public void DisallowCastle(PieceColor color, CastleType castleType)
    {
        var castleKey = GetCastleKey(color, castleType);
        _value &= ~castleKey;
    }

    private static int GetCastleKey(PieceColor color, CastleType castleType)
    {
        return color == PieceColor.Black ? (int)castleType : (int)castleType << 2;
    }
}