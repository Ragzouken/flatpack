using UnityEngine;
using UnityEditor;
using NUnit.Framework;

public class DrawingTests
{
    private int Difference(Texture2D a, Texture2D b)
    {
        a.Apply();
        b.Apply();

        Color32[] pixelsA = a.GetPixels32();
        Color32[] pixelsB = b.GetPixels32();

        Assert.AreEqual(pixelsA.Length, pixelsB.Length, string.Format("Texture {0} is not the same size as Texture {1}!", a.name, b.name));

        int difference = 0;

        for (int i = 0; i < pixelsA.Length; ++i)
        {
            bool equal = pixelsA[i].r == pixelsB[i].r
                      && pixelsA[i].g == pixelsB[i].g
                      && pixelsA[i].b == pixelsB[i].b
                      && pixelsA[i].a == pixelsB[i].a;

            difference += equal ? 0 : 1;
        }

        return difference;
    }

    private int Difference<TTexture, TPixel>(ManagedTexture<TTexture, TPixel>.Sprite a, 
                                             ManagedTexture<TTexture, TPixel>.Sprite b)
        where TTexture : ManagedTexture<TTexture, TPixel>, new()
    {
        Color[] pixelsA = GetPixels(a);
        Color[] pixelsB = GetPixels(b);

        Assert.AreEqual(pixelsA.Length, pixelsB.Length, string.Format("Sprite {0} is not the same size as Sprite {1}!", a, b));

        int difference = 0;

        for (int i = 0; i < pixelsA.Length; ++i)
        {
            Color32 ca = pixelsA[i];
            Color32 cb = pixelsB[i];

            bool equal = ca.r == cb.r
                      && ca.g == cb.g
                      && ca.b == cb.b
                      && ca.a == cb.a;

            difference += equal ? 0 : 1;
        }

        return difference;
    }

    private void SaveOut(Sprite sprite, string name)
    {
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Tests Output/");
        sprite.texture.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Tests Output/" + name + ".png", sprite.texture.EncodeToPNG());
        AssetDatabase.Refresh();
    }

    private void SaveOut<TTexture, TPixel>(ManagedTexture<TTexture, TPixel>.Sprite sprite, string name)
        where TTexture : ManagedTexture<TTexture, TPixel>, new()
    {
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Tests Output/");
        sprite.mTexture.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Tests Output/" + name + ".png", sprite.mTexture.uTexture.EncodeToPNG());
        AssetDatabase.Refresh();
    }

    private Color[] GetPixels<TTexture, TPixel>(ManagedTexture<TTexture, TPixel>.Sprite sprite)
        where TTexture : ManagedTexture<TTexture, TPixel>, new()
    {
        sprite.mTexture.Apply();

        return sprite.mTexture.uTexture.GetPixels(sprite.uSprite.textureRect);
    }

    private Texture2D GetExact<TTexture, TPixel>(ManagedTexture<TTexture, TPixel>.Sprite sprite)
        where TTexture : ManagedTexture<TTexture, TPixel>, new()
    {
        var tex = Texture2DExtensions.Blank((int) sprite.rect.width, (int) sprite.rect.height);
        tex.SetPixels(GetPixels(sprite));
        tex.Apply();

        return tex;
    }

    private void SaveOutExact<TTexture, TPixel>(ManagedTexture<TTexture, TPixel>.Sprite sprite, string name)
        where TTexture : ManagedTexture<TTexture, TPixel>, new()
    {
        SaveOut(GetExact(sprite).FullSprite(), name);
        AssetDatabase.Refresh();
    }

    [Test]
    public void Reference01_DrawTools()
    {
        var reference = Resources.Load<Texture2D>("Drawing-Reference-01");

        Assert.AreEqual(Difference(reference, reference), 0, "Reference image doesn't equal itself!");

        var canvas = TextureColor.Draw.GetSprite(64, 64);
        canvas.Clear(Color.white);
        TextureColor.Draw.Circle(canvas, Vector2.one * 4, Color.black, 3, TextureColor.alpha);

        Assert.AreNotEqual(Difference(reference, GetExact(canvas)), 0, "Generated image should be different to reference at this point!");

        TextureColor.Draw.Line(canvas, new Vector2(8, 4), new Vector2(12, 4), Color.black, 3, TextureColor.alpha);
        TextureColor.Draw.Line(canvas, new Vector2(4, 8), new Vector2(8, 12), Color.black, 3, TextureColor.alpha);

        TextureColor.Draw.Circle(canvas, new Vector2( 5, 17), Color.black, 4, TextureColor.alpha);
        TextureColor.Draw.Circle(canvas, new Vector2( 5, 25), Color.black, 4, TextureColor.alpha);
        TextureColor.Draw.Circle(canvas, new Vector2(13, 17), Color.black, 4, TextureColor.alpha);
        TextureColor.Draw.Circle(canvas, new Vector2(13, 25), Color.black, 4, TextureColor.alpha);

        TextureColor.Draw.Circle(canvas, new Vector2(23, 11), Color.black, 16, TextureColor.alpha);

        TextureColor.Draw.Line(canvas, new Vector2(35, 3), new Vector2(59, 3), Color.red,   6, TextureColor.alpha);
        TextureColor.Draw.Line(canvas, new Vector2(35, 3), new Vector2(59, 3), Color.blue,  4, TextureColor.alpha);
        TextureColor.Draw.Line(canvas, new Vector2(35, 3), new Vector2(59, 3), Color.green, 2, TextureColor.alpha);

        int difference = Difference(reference, GetExact(canvas));

        SaveOutExact(canvas, "Drawing-Reference-01-Managed");

        Assert.AreEqual(difference, 0, string.Format("Generated image doesn't match reference! ({0} difference)", difference));
    }
}
