using System.Runtime.CompilerServices;

namespace MonsterTCG.Util;

public static class TupleUtil
{
    public static List<T?> GetListFromTuple<T>(ITuple tuple)
    {
        return Enumerable.Range(0, tuple.Length)
            .Select(i => (T?)tuple[i])
            .ToList();
    }
}