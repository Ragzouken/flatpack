using UnityEngine;

public static partial class SpriteExtensions
{
    public static Color GetPixel(this Sprite sprite,
                                 Vector2 position)
    {
        position += sprite.textureRect.position;

        return sprite.textureRect.Contains(position)
             ? sprite.texture.GetPixel((int) position.x, (int) position.y)
             : Color.clear;
    }

    public static Color[] GetPixels(this Sprite sprite)
    {
        return sprite.texture.GetPixels(sprite.textureRect);
    }

    public static void SetPixels(this Sprite sprite, Color[] colors)
    {
        sprite.texture.SetPixels(sprite.textureRect, colors);
    }

    public static void Apply(this Sprite sprite)
    {
        sprite.texture.Apply();
    }
}

public static partial class Texture2DExtensions
{
    public static Texture2D Blank(int width, 
                                  int height,
                                  TextureFormat format=TextureFormat.ARGB32)
    {
        var texture = new Texture2D(width, height, format, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        return texture;
    }

    public static Sprite FullSprite(this Texture2D texture,
                                    Vector2 pivot = default(Vector2),
                                    int pixelsPerUnit = 1)
    {
        var rect = new Rect(0, 0, texture.width, texture.height);

        Sprite sprite = Sprite.Create(texture,
                                      rect,
                                      pivot,
                                      pixelsPerUnit,
                                      0U,
                                      SpriteMeshType.FullRect);

        return sprite;
    }

    public static void Clear(this Texture2D texture, Color color)
    {
        int size = texture.width * texture.height;
        var bytes = new byte[4 * size];

        for (int i = 0; i < 4 * size; i += 4)
        {
            bytes[i + 0] = (byte) (color.a * 255);
            bytes[i + 1] = (byte) (color.r * 255);
            bytes[i + 2] = (byte) (color.g * 255);
            bytes[i + 3] = (byte) (color.b * 255);
        }

        texture.LoadRawTextureData(bytes);
    }

    public static Color[] GetPixels(this Texture2D texture, 
                                    IntRect rect)
    {
        return texture.GetPixels(rect.x, 
                                 rect.y, 
                                 rect.width, 
                                 rect.height);
    }

    public static void SetPixels(this Texture2D texture, 
                                 IntRect rect, 
                                 Color[] pixels)
    {
        texture.SetPixels(rect.x, 
                          rect.y, 
                          rect.width, 
                          rect.height, 
                          pixels);
    }
}

// Author: Jason Morley (Source: http://www.morleydev.co.uk/blog/2010/11/18/generic-bresenhams-line-algorithm-in-visual-basic-net/)
// Licence: Public Domain
public static class Bresenham
{
    private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

    public delegate void PlotFunction(int x, int y);

    public static void Line(int x0, int y0, int x1, int y1, PlotFunction plot)
    {
        bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);

        if (steep) { Swap(ref x0, ref y0); Swap(ref x1, ref y1); }
        if (x0 > x1) { Swap(ref x0, ref x1); Swap(ref y0, ref y1); }

        int dX = (x1 - x0);
        int dY = Mathf.Abs(y1 - y0);
        int err = (dX / 2);
        int ystep = (y0 < y1 ? 1 : -1);
        int y = y0;

        for (int x = x0; x <= x1; ++x)
        {
            if (steep) plot(y, x);
            else plot(x, y);

            err = err - dY;

            if (err < 0)
            {
                y += ystep;
                err += dX;
            }
        }
    }
}
