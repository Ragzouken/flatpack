using System;
using UnityEngine;

public class TextureByte : ManagedTexture<TextureByte, byte>
{
    public static Blend<byte> mask = (canvas, brush) => brush == 0 ? canvas : brush;
    public static DrawTools Draw = new DrawTools(mask, 0);

    public static byte Lerp(byte a, byte b, byte u)
    {
        return (byte)(a + ((u * (b - a)) >> 8));
    }

    public static Texture2D temporary;

    static TextureByte()
    {
        temporary = Texture2DExtensions.Blank(1, 1);
    }

    public TextureByte() : base(TextureFormat.Alpha8) { }

    public TextureByte(int width, int height) 
        : base(width, height, TextureFormat.Alpha8)
    {
    }

    public override void ApplyPixels()
    {
        uTexture.LoadRawTextureData(pixels);
    }

    public void SetPixels32(Color32[] pixels)
    {
        for (int i = 0; i < this.pixels.Length; ++i)
        {
            this.pixels[i] = pixels[i].a;
        }

        dirty = true;
    }

    public void DecodeFromPNG(byte[] data)
    {
        temporary.LoadImage(data);
        SetPixels32(temporary.GetPixels32());

        Apply();
    }
}
