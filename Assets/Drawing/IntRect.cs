using UnityEngine;

public struct IntRect
{
    public int xMin;
    public int yMin;
    public int xMax;
    public int yMax;

    public int x { get { return xMin; } }
    public int y { get { return yMin; } }
    public int width { get { return xMax - xMin; } }
    public int height { get { return yMax - yMin; } }

    public IntRect(int x, int y, int width, int height)
    {
        xMin = x;
        yMin = y;
        xMax = x + width;
        yMax = y + height;
    }

    public static implicit operator IntRect(Rect rect)
    {
        return new IntRect
        {
            xMin = (int) rect.xMin,
            yMin = (int) rect.yMin,
            xMax = (int) rect.xMax,
            yMax = (int) rect.yMax,
        };
    }

    public static implicit operator Rect(IntRect rect)
    {
        return Rect.MinMaxRect(rect.xMin, rect.yMin, rect.xMax, rect.yMax);
    }

    public void Move(int dx, int dy)
    {
        xMin += dx;
        xMax += dx;
        yMin += dy;
        yMax += dy;
    }

    public void Move(IntVector2 offset)
    {
        Move(offset.x, offset.y);
    }

    public bool Contains(int x, int y)
    {
        return x >= xMin && x < xMax
            && y >= yMin && y < yMax;
    }

    public bool Contains(IntVector2 position)
    {
        return Contains(position.x, position.y);
    }

    public IntRect Intersect(IntRect b)
    {
        var a = this;

        a.xMin = Mathf.Max(a.xMin, b.xMin);
        a.yMin = Mathf.Max(a.yMin, b.yMin);
        a.xMax = Mathf.Min(a.xMax, b.xMax);
        a.yMax = Mathf.Min(a.yMax, b.yMax);

        return a;
    }

    public bool Intersects(IntRect b)
    {
        var i = Intersect(b);

        return i.width  > 0 
            && i.height > 0;
    }

    public void Expand(int size)
    {
        xMin -= size;
        yMin -= size;
        xMax += size;
        yMax += size;
    }

    public override string ToString()
    {
        return string.Format("(x: {0}, y: {1}, width: {2}, height: {3})", x, y, width, height);
    }
}
