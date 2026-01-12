// 定义常用方向，方便调用 (可以放在静态类或常量类里)

using System.Collections.Generic;
using UnityEngine;

public static class GridDirections
{
    public static readonly Vector2Int[] All = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    public static readonly Vector2Int[] Top = { Vector2Int.up };
    public static readonly Vector2Int[] Bottom = { Vector2Int.down };
    public static readonly Vector2Int[] Left = { Vector2Int.left };
    public static readonly Vector2Int[] Right = { Vector2Int.right };
    // 甚至可以扩展：
    public static readonly Vector2Int[] Horizontal = { Vector2Int.left, Vector2Int.right };
}

