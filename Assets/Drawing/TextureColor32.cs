using UnityEngine;

public class TextureColor32 : ManagedTexture<TextureColor32, Color32>
{
    public static Blend<Color32> mask     = (canvas, brush) => brush.a > 0 ? brush : canvas;
    public static Blend<Color32> alpha    = (canvas, brush) => Lerp(canvas, brush, brush.a);
    public static Blend<Color32> replace  = (canvas, brush) => brush;

    public static Blend<Color32> stencilKeep = (canvas, brush) => Lerp(Color.clear, canvas, brush.a);
    public static Blend<Color32> stencilCut  = (canvas, brush) => Lerp(canvas, Color.clear, brush.a);

    public static DrawTools Draw = new DrawTools(mask, Color.clear);

    public static byte Lerp(byte a, byte b, byte u)
    {
        return (byte)(a + ((u * (b - a)) >> 8));
    }

    public static Color32 Lerp(Color32 a, Color32 b, byte u)
    {
        a.a = Lerp(a.a, b.a, u);
        a.r = Lerp(a.r, b.r, u);
        a.g = Lerp(a.g, b.g, u);
        a.b = Lerp(a.b, b.b, u);

        return a;
    }

    public TextureColor32() : base(TextureFormat.ARGB32) { }

    public TextureColor32(int width, int height)
        : base(width, height, TextureFormat.ARGB32)
    {
    }

    public override void ApplyPixels()
    {
        uTexture.SetPixels32(pixels);
    }
}
