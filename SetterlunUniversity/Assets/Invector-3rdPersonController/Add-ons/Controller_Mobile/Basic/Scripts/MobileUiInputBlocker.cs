using System.Collections.Generic;

public static class MobileUiInputBlocker
{
    private static readonly HashSet<int> activePointers = new HashSet<int>();

    public static bool IsBlockingPointClick => activePointers.Count > 0;

    public static void BeginBlock(int pointerId)
    {
        activePointers.Add(pointerId);
    }

    public static void EndBlock(int pointerId)
    {
        activePointers.Remove(pointerId);
    }

    public static void Clear()
    {
        activePointers.Clear();
    }
}
