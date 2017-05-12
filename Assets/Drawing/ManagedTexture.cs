using UnityEngine;
using System;
using System.Collections.Generic;

public delegate TPixel Blend<TPixel>(TPixel canvas, TPixel brush);

public abstract partial class ManagedTexture<TTexture, TPixel> : IDisposable
    where TTexture : ManagedTexture<TTexture, TPixel>, new()
{
    public int width;
    public int height;
    public TextureFormat format;

    public Texture2D uTexture;
    public TPixel[] pixels;
    public bool dirty;

    public static Blend<TPixel> CopyBlend = (canvas, brush) => brush;

    /// <summary>
    /// Return a new sprite that encompasses the full extent of this texture,
    /// with the given pivot.
    /// </summary>
    public Sprite FullSprite(IntVector2 pivot)
    {
        return new Sprite(this as TTexture, new IntRect(0, 0, width, height), pivot);
    }

    protected ManagedTexture(TextureFormat format = default(TextureFormat))
    {
        this.format = format;
    }

    protected ManagedTexture(int width, int height, TextureFormat format)
    {
        Reformat(width, height, format);
    }

    /// <summary>
    /// Change dimensions of this texture. Erases all pixel data.
    /// </summary>
    public void Resize(int width, int height)
    {
        this.width = width;
        this.height = height;

        pixels = new TPixel[width * height];
        dirty = true;

        if (uTexture != null)
        {
            UnityEngine.Object.Destroy(uTexture);
            uTexture = null;
        }
    }

    /// <summary>
    /// Change dimensions and format of this texture. Erases all pixel data.
    /// </summary>
    public void Reformat(int width, int height, TextureFormat format)
    {
        this.format = format;

        Resize(width, height);
    }

    /// <summary>
    /// Use a given blend function to blend the pixel values of a rect of
    /// another texture onto a rect of pixels values in this texture.
    /// </summary>
    public void Blend(ManagedTexture<TTexture, TPixel> brush,
                      Blend<TPixel> blend,
                      IntRect canvasRect,
                      IntRect brushRect)
    {
        int dx = brushRect.xMin - canvasRect.xMin;
        int dy = brushRect.yMin - canvasRect.yMin;

        int cstride = width;
        int bstride = brush.width;

        int xmin = canvasRect.xMin;
        int ymin = canvasRect.yMin;
        int xmax = canvasRect.xMax;
        int ymax = canvasRect.yMax;

        for (int cy = ymin; cy < ymax; ++cy)
        {
            for (int cx = xmin; cx < xmax; ++cx)
            {
                int bx = cx + dx;
                int by = cy + dy;

                int ci = cy * cstride + cx;
                int bi = by * bstride + bx;

                pixels[ci] = blend(pixels[ci], brush.pixels[bi]);
            }
        }

        // pixel data changed, mark the texture as needing an update
        dirty = true;
    }

    /// <summary>
    /// Return an array of all the pixel values in this texture. If an array is 
    /// provided then it will be filled and returned instead of creating a new 
    /// one.
    /// </summary>
    public TPixel[] GetPixels(TPixel[] copy=null)
    {
        copy = copy ?? new TPixel[pixels.Length];

        Array.Copy(pixels, copy, pixels.Length);

        return copy;
    }

    /// <summary>
    /// Return an array of a rect of the pixel values in this texture. If an 
    /// array is provided then it will be filled and returned instead of 
    /// creating a new one.
    /// </summary>
    public TPixel[] GetPixels(IntRect rect, TPixel[] copy=null)
    {
        copy = copy ?? new TPixel[rect.width * rect.height];

        int tstride = width;
        int cstride = rect.width;

        // copy row by row
        for (int cy = 0; cy < rect.height; ++cy)
        {
            int tx = rect.xMin;
            int ty = cy + rect.yMin;

            int cx = 0;

            Array.Copy(pixels, ty * tstride + tx, 
                       copy,   cy * cstride + cx, 
                       cstride);
        }

        return copy;
    }

    /// <summary>
    /// Overwrite all pixel values in this texture with those in a given array.
    /// </summary>
    public void SetPixels(TPixel[] pixels)
    {
        Array.Copy(pixels, this.pixels, this.pixels.Length);

        // pixel data changed, mark the texture as needing an update
        dirty = true;
    }

    /// <summary>
    /// Overwrite a rect pixel values in this texture with those in a given 
    /// array.
    /// </summary>
    public void SetPixels(IntRect rect, TPixel[] copy)
    {
        int tstride = width;
        int cstride = rect.width;

        // overwrite row by row
        for (int cy = 0; cy < rect.height; ++cy)
        {
            int tx = rect.xMin;
            int ty = rect.yMin + cy;

            int cx = 0;

            Array.Copy(copy,   cy * cstride + cx, 
                       pixels, ty * tstride + tx, 
                       cstride);
        }

        // pixel data changed, mark the texture as needing an update
        dirty = true;
    }

    /// <summary>
    /// Replace all pixels of this texture with either a given pixel value
    /// </summary>
    public void Clear(TPixel value, IntRect rect)
    {
        int stride = width;

        int xmin = rect.xMin;
        int ymin = rect.yMin;
        int xmax = rect.xMax;
        int ymax = rect.yMax;

        for (int y = ymin; y < ymax; ++y)
        {
            for (int x = xmin; x < xmax; ++x)
            {
                int i = y * stride + x;

                pixels[i] = value;
            }
        }

        // pixel data changed, mark the texture as needing an update
        dirty = true;
    }

    /// <summary>
    /// Replace all pixels of this texture with either a given pixel value or 
    /// the default pixel value.
    /// </summary>
    public void Clear(TPixel value = default(TPixel))
    {
        for (int i = 0; i < pixels.Length; ++i)
        {
            pixels[i] = value;
        }

        // pixel data changed, mark the texture as needing an update
        dirty = true;
    }

    // Return the pixel value at the given coordinates.
    //
    // You should avoid using this if you need to access large chunks of the
    // texture - access pixels directly instead!
    public TPixel GetPixel(int x, int y)
    {
        return pixels[width * y + x];
    }

    // Overwrite the pixel value at the given coordinates.
    //
    // You should avoid using this if you need to access large chunks of the
    // texture - access pixels directly instead!
    public void SetPixel(int x, int y, TPixel value)
    {
        pixels[width * y + x] = value;

        // pixel data changed, mark the texture as needing an update
        dirty = true;
    }

    /// <summary>
    /// Copy current pixel data to the underlying Unity Texture if there are
    /// any changes. If necessary, create the underlying Unity Texture.
    /// </summary>
    public void Apply()
    {
        // if the Unity Texture2D doesn't exist yet, create it
        if (uTexture == null)
        {
            uTexture = Texture2DExtensions.Blank(width, height, format);
            dirty = true;
        }

        // only copy pixel data if it has changed since last time
        if (dirty)
        {
            ApplyPixels();
            uTexture.Apply();
            dirty = false;
        }
    }

    /// <summary>
    /// Copy all pixel data into to the underlying Unity Texture
    /// </summary>
    public abstract void ApplyPixels();

    /// <summary>
    /// Destroy the corresponding Unity Texture2D, if it was ever created. They
    /// do not get garbage collected!
    /// </summary>
    public virtual void Dispose()
    {
        UnityEngine.Object.Destroy(uTexture);
        uTexture = null;
    }
}
