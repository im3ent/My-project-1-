using System.Collections.Generic;
using Vector2Int = UnityEngine.Vector2Int;

// 建议扩充你的枚举
public enum ItemShapeType
{
    // --- 基础矩形 ---
    SquareMissingCornerTopRight,    // 2x2 少右上角
    SquareMissingCornerTopLeft,     // 2x2 少左上角
    SquareMissingCornerBottomRight, // 2x2 少右下角
    SquareMissingCornerBottomLeft,  // 2x2 少左下角

    // --- L 型 (3x2) ---
    LShape0,   // L 正常
    LShape90,  // L 卧倒
    LShape180, // L 倒立
    LShape270, // L 卧倒反向

    // --- 凸字型 (3x3中心突起) ---
    ProtrusionUp,    // 凸
    ProtrusionDown,  // 凹 (倒过来的凸)
    ProtrusionLeft,  // 向左指
    ProtrusionRight, // 向右指

    // --- C 型 (2x3 窄版) ---
    CShape2X3Right, // 开口向右 [
    CShape2X3Left,  // 开口向左 ]
    CShape2X3Up,    // 开口向上 (U型)
    CShape2X3Down,  // 开口向下 (门型)

    // --- C 型 (3x3 宽版) ---
    CShape3X3Right,
    CShape3X3Left,
    CShape3X3Up,
    CShape3X3Down,

    // --- 特殊形状 ---
    LightningA,     // 闪电/Z字型 A
    LightningB,     // 闪电/Z字型 B (镜像)
    Cross            // 十字架
}

public static class ShapeLibrary
{
    public static Dictionary<ItemShapeType, List<Vector2Int>> GetShapes()
    {
        var lib = new Dictionary<ItemShapeType, List<Vector2Int>>
        {
            // ==========================================
            // 1. 正方形少一个角 (2x2)
            // ==========================================
            // [X][ ]  (少右上)
            // [X][X]
            [ItemShapeType.SquareMissingCornerTopRight] = new List<Vector2Int> {
                new Vector2Int(0,0), 
                new Vector2Int(0,1), new Vector2Int(1,1)
            },
            // [ ][X]  (少左上)
            // [X][X]
            [ItemShapeType.SquareMissingCornerTopLeft] = new List<Vector2Int> {
                new Vector2Int(1,0),
                new Vector2Int(0,1), new Vector2Int(1,1)
            },
            // [X][X]  (少右下)
            // [X][ ]
            [ItemShapeType.SquareMissingCornerBottomRight] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(0,1)
            },
            // [X][X]  (少左下)
            // [ ][X]
            [ItemShapeType.SquareMissingCornerBottomLeft] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(1,1)
            },
            // ==========================================
            // 2. L型 (3x2 边界) - 类似俄罗斯方块 L
            // ==========================================
            // [X][ ]
            // [X][ ]
            // [X][X]
            [ItemShapeType.LShape0] = new List<Vector2Int> {
                new Vector2Int(0,0),
                new Vector2Int(0,1),
                new Vector2Int(0,2), new Vector2Int(1,2)
            },
            // [X][X][X]
            // [X][ ][ ]
            [ItemShapeType.LShape90] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(0,1)
            },
            // [X][X]
            // [ ][X]
            // [ ][X]
            [ItemShapeType.LShape180] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(1,1),
                new Vector2Int(1,2)
            },
            //     [ ][ ][X]
            // [X][X][X]
            [ItemShapeType.LShape270] = new List<Vector2Int> {
                new Vector2Int(2,0),
                new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
            },
            // ==========================================
            // 3. 凸字型 / 坦克型 (3x2 或 3x3)
            // ==========================================
            //   [X]    (凸)
            // [X][X][X]
            [ItemShapeType.ProtrusionUp] = new List<Vector2Int> {
                new Vector2Int(1,0),
                new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
            },
            // [X][X][X] (倒凸)
            //   [X]
            [ItemShapeType.ProtrusionDown] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(1,1)
            },
            //   [X]
            // [X][X]  (向左)
            //   [X]
            [ItemShapeType.ProtrusionLeft] = new List<Vector2Int> {
                new Vector2Int(1,0),
                new Vector2Int(0,1), new Vector2Int(1,1),
                new Vector2Int(1,2)
            },
            // [X]
            // [X][X]  (向右)
            // [X]
            [ItemShapeType.ProtrusionRight] = new List<Vector2Int> {
                new Vector2Int(0,0),
                new Vector2Int(0,1), new Vector2Int(1,1),
                new Vector2Int(0,2)
            },
            // ==========================================
            // 4. 窄 C 型 (2x3)
            // ==========================================
            // [X][X]
            // [X][ ]  (开口向右)
            // [X][X]
            [ItemShapeType.CShape2X3Right] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(0,1),
                new Vector2Int(0,2), new Vector2Int(1,2)
            },
            // [X][X]
            // [ ][X]  (开口向左)
            // [X][X]
            [ItemShapeType.CShape2X3Left] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(1,1),
                new Vector2Int(0,2), new Vector2Int(1,2)
            },
            // [X][ ][X] (3宽 2高 U型)
            // [X][X][X]
            [ItemShapeType.CShape2X3Up] = new List<Vector2Int> {
                new Vector2Int(0,0),                      new Vector2Int(2,0),
                new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
            },
            // [X][X][X] (3宽 2高 门型)
            // [X][ ][X]
            [ItemShapeType.CShape2X3Down] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(0,1),                      new Vector2Int(2,1)
            },
            // ==========================================
            // 5. 宽 C 型 (3x3)
            // ==========================================
            // [X][X][X]
            // [X][ ][ ] (开口向右)
            // [X][X][X]
            [ItemShapeType.CShape3X3Right] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(0,1),
                new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2)
            },
            // [X][X][X]
            // [ ][ ][X] (开口向左)
            // [X][X][X]
            [ItemShapeType.CShape3X3Left] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(2,1),
                new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2)
            },
            // [X][ ][X]
            // [X][ ][X] (开口向上 U)
            // [X][X][X]
            [ItemShapeType.CShape3X3Up] = new List<Vector2Int> {
                new Vector2Int(0,0),                      new Vector2Int(2,0),
                new Vector2Int(0,1),                      new Vector2Int(2,1),
                new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2)
            },
            // [X][X][X]
            // [X][ ][X] (开口向下 门)
            // [X][ ][X]
            [ItemShapeType.CShape3X3Down] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(0,1),                      new Vector2Int(2,1),
                new Vector2Int(0,2),                      new Vector2Int(2,2)
            },
            // ==========================================
            // 6. 闪电型 / Z字型 (3x2)
            // ==========================================
            // [X][X][ ]
            // [ ][X][X]
            [ItemShapeType.LightningA] = new List<Vector2Int> {
                new Vector2Int(0,0), new Vector2Int(1,0),
                new Vector2Int(1,1), new Vector2Int(2,1)
            },
            // [ ][X][X]
            // [X][X][ ]
            [ItemShapeType.LightningB] = new List<Vector2Int> {
                new Vector2Int(1,0), new Vector2Int(2,0),
                new Vector2Int(0,1), new Vector2Int(1,1)
            },
            // ==========================================
            // 7. 十字架 (3x3)
            // ==========================================
            //   [X]
            // [X][X][X]
            //   [X]
            [ItemShapeType.Cross] = new List<Vector2Int> {
                new Vector2Int(1,0),
                new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1),
                new Vector2Int(1,2)
            }
        };

        return lib;
    }
}