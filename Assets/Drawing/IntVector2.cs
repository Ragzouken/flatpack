using UnityEngine;

public struct IntVector2 : System.IEquatable<IntVector2>
{
    public int x;
    public int y;

    public static IntVector2 zero = new IntVector2(0, 0);
    public static IntVector2 one  = new IntVector2(1, 1);

    public static IntVector2 left  = new IntVector2(-1,  0);
    public static IntVector2 right = new IntVector2( 1,  0);
    public static IntVector2 up    = new IntVector2( 0,  1);
    public static IntVector2 down  = new IntVector2( 0, -1);

    public static IntVector2[] ortho = new IntVector2[]
    {
        right, down, left, up
    };

    public static IntVector2[] adjacent8 = new IntVector2[]
    {
        right, right + down, down, down + left, left, left + up, up, up + right,
    };

    public IntVector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public IntVector2(float x, float y) : this((int) x, (int) y) { }

    public void GridCoords(int cellSize,
                           out IntVector2 cell,
                           out IntVector2 local)
    {
        cell = CellCoords(cellSize);
        local = OffsetCoords(cellSize);
    }

    public IntVector2 CellCoords(int cellSize)
    {
        return new IntVector2(Mathf.FloorToInt(x / (float) cellSize),
                              Mathf.FloorToInt(y / (float) cellSize));
    }

    public IntVector2 OffsetCoords(int cellSize)
    {
        float ox = x % cellSize;
        float oy = y % cellSize;

        return new IntVector2(ox >= 0 ? ox : cellSize + ox,
                              oy >= 0 ? oy : cellSize + oy);
    }

    public IntVector2 Moved(int dx, int dy)
    {
        return new IntVector2(x + dx, y + dy);
    }

    public static implicit operator Vector2(IntVector2 point)
    {
        return new Vector2(point.x, point.y);
    }

    public static implicit operator Vector3(IntVector2 point)
    {
        return new Vector3(point.x, point.y, 0);
    }

    public static implicit operator IntVector2(Vector2 vector)
    {
        return new IntVector2(vector.x, vector.y);
    }

    public static implicit operator IntVector2(Vector3 vector)
    {
        return new IntVector2(vector.x, vector.y);
    }

    public override bool Equals (object obj)
    {
        if (obj is IntVector2)
        {
            return Equals((IntVector2) obj);
        }

        return false;
    }

    public bool Equals(IntVector2 other)
    {
        return other.x == x
            && other.y == y;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + x.GetHashCode();
        hash = hash * 23 + y.GetHashCode();
        return hash;
    }

    public override string ToString ()
    {
        return string.Format("(x: {0}, y: {1})", x, y);
    }

    public static bool operator ==(IntVector2 a, IntVector2 b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(IntVector2 a, IntVector2 b)
    {
        return a.x != b.x || a.y != b.y;
    }

    public static IntVector2 operator +(IntVector2 a, IntVector2 b)
    {
        a.x += b.x;
        a.y += b.y;

        return a;
    }

    public static IntVector2 operator -(IntVector2 a, IntVector2 b)
    {
        a.x -= b.x;
        a.y -= b.y;

        return a;
    }
    
    public static IntVector2 operator *(IntVector2 a, int scale)
    {
        a.x *= scale;
        a.y *= scale;

        return a;
    }
}
