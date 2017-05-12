using UnityEngine;

public class TextureColor : ManagedTexture<TextureColor, Color>
{
    public static Blend<Color> mask     = (canvas, brush) => brush.a > 0 ? brush : canvas;
    public static Blend<Color> alpha    = (canvas, brush) => Lerp(canvas, brush, brush.a);
    public static Blend<Color> add      = (canvas, brush) => canvas + brush;
    public static Blend<Color> subtract = (canvas, brush) => canvas - brush;
    public static Blend<Color> multiply = (canvas, brush) => canvas * brush;
    public static Blend<Color> replace  = (canvas, brush) => brush;

    public static Blend<Color> stencilKeep = (canvas, brush) => Lerp(Color.clear, canvas, brush.a);
    public static Blend<Color> stencilCut  = (canvas, brush) => Lerp(canvas, Color.clear, brush.a);

    public static DrawTools Draw = new DrawTools(mask, Color.clear);

    public static Color Lerp(Color a, Color b, float u)
    {
        a.a = a.a * (1 - u) + b.a * u;
        a.r = a.r * (1 - u) + b.r * u;
        a.g = a.g * (1 - u) + b.g * u;
        a.b = a.b * (1 - u) + b.b * u;

        return a;
    }

    public TextureColor() : base(TextureFormat.ARGB32) { }

    public TextureColor(int width, int height)
        : base(width, height, TextureFormat.ARGB32)
    {
    }

    public override void ApplyPixels()
    {
        uTexture.SetPixels(pixels);
    }
}
