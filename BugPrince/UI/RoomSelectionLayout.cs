using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.UI;

internal record RoomSelectionLayoutPosition
{
    public int Index;
    public Vector2 Pos;
    public int? LeftIndex;
    public int? RightIndex;
    public int? DownIndex;
    public int? UpIndex;
}

internal class RoomSelectionLayout
{
    private enum RowType
    {
        Mid,
        Top,
        Bottom,
    }

    private const float X_SPACE = 8;
    private const float Y_SPACE = 6;

    private static float XPos(int i, int size) => (i - (size - 1f) / 2) * X_SPACE;

    private static float YPos(RowType rowType) => rowType switch { RowType.Mid => 0, RowType.Top => -Y_SPACE / 2, RowType.Bottom => Y_SPACE / 2, _ => throw new System.ArgumentException($"Invalid rowType: {rowType}") ;

    private List<RoomSelectionLayoutPosition> positions = [];

    public RoomSelectionLayout(int size)
    {
        if (size < 1 || size > 6) throw new System.ArgumentException($"Bad size: {size}");

        if (size <= 3) AddRow(RowType.Mid, size);
        else
        {
            int top = (size + 1) / 2;
            AddRow(RowType.Top, top);
            AddRow(RowType.Bottom, size - top);
        }

        MirrorDirections();
        if (size == 5) positions[2].DownIndex = 4;
    }

    public RoomSelectionLayoutPosition this[int idx] => positions[idx] with { };

    private void AddRow(RowType rowType, int size)
    {
        var yPos = YPos(rowType);

        int topRowSize = positions.Count;
        int idx = positions.Count;
        for (int i = 0; i < size; i++)
        {
            RoomSelectionLayoutPosition pos = new()
            {
                Index = idx,
                Pos = new(XPos(i, size), yPos),
            };

            if (i > 0) pos.LeftIndex = idx - 1;
            if (rowType == RowType.Bottom) pos.UpIndex = idx - topRowSize;

            positions.Add(pos);
            idx++;
        }
    }

    private void MirrorDirections()
    {
        foreach (var pos in positions)
        {
            if (pos.LeftIndex.HasValue) positions[pos.LeftIndex.Value].RightIndex = pos.Index;
            if (pos.UpIndex.HasValue) positions[pos.UpIndex.Value].DownIndex = pos.Index;
        }
    }
}
