using Sirenix.OdinInspector;
using UnityEngine;
using Extensions;
using System.Collections.Generic;
using System;

public struct HexGrid
{
    public const int cols = Index.cols;
    public const int maxCol = Index.maxCol;
    public const int rows = Index.rows;
    public const int maxRow = Index.maxRow;
    public float radius;
    public float height;
    [SerializeField, MinMaxSlider(-64, 64, true)] private Vector2 hexHeightVariance;
    [ShowInInspector, ReadOnly] public float minHeight => hexHeightVariance.x;
    [ShowInInspector, ReadOnly] public float maxHeight => hexHeightVariance.y;

    public List<Color> colors;

    public float Apothem =>
       (radius.Sqr() - (radius * 0.5f).Sqr()).Sqrt();

    public static bool IsInBounds(int row, int col) => IsInBounds(new Index(row, col));
    public static bool IsInBounds(Index index) => index.IsInBounds;

    public static bool GetValidIndex(int row, int col, out Index index)
    {
        if (!IsInBounds(row, col)) {
            index = default;
            return false;
        }
        index = new Index(row, col);
        return true;
    }

    public static Index? GetNeighborAt(Index source, HexNeighborDirection dir)
    {
        return source.GetNeighborAt(dir);
    }
}