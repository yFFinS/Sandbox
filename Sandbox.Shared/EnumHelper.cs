namespace Sandbox.Shared;

public static class EnumHelper
{
    public static Dictionary<TEnum, TValue?> CreateValueMap<TEnum, TValue>() where TEnum : struct, Enum
    {
        var map = new Dictionary<TEnum, TValue?>();
        foreach (var value in Enum.GetValues(typeof(TEnum)))
        {
            map[(TEnum)value] = default;
        }

        return map;
    }
}