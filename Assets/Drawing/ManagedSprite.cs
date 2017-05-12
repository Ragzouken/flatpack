using UnityEngine;
using System;
using System.Collections.Generic;

public partial class ManagedTexture<TTexture, TPixel>
{
    public class Sprite : IDisposable
    {
        public TTexture mTexture;

        private UnityEngine.Sprite _uSprite;
        /// <summary>
        /// The corresponding Unity Sprite. Will be created if it doesn't exist yet.
        /// </summary>
        public UnityEngine.Sprite uSprite
        {
            get
            {
                if (_uSprite == null)
                {
                    // TODO: what if the Unity Texture doesn't exist yet
                    // should we store pixels per unit?

                    Vector2 piv;
                    piv.x = pivot.x / (float) rect.width;
                    piv.y = pivot.y / (float) rect.height;

                    _uSprite = UnityEngine.Sprite.Create(mTexture.uTexture, rect, piv, 1, 0, SpriteMeshType.FullRect);
                }

                return _uSprite;
            }

            set
            {
                _uSprite = value;
            }
        }

        public IntRect rect;
        public IntVector2 pivot;

        protected Sprite() { }

        public Sprite(TTexture mTexture,
                             IntRect rect,
                             IntVector2 pivot)
        {
            this.mTexture = mTexture;
            this.rect = rect;
            this.pivot = pivot;
        }

        public Sprite(TTexture mTexture,
                             UnityEngine.Sprite sprite)
        {
            this.mTexture = mTexture;
            rect = sprite.textureRect;
            pivot = sprite.pivot;

            uSprite = sprite;
        }

        /// <summary>
        /// Use a given blend function to blend the pixel values of a given brush 
        /// sprite onto this sprite, using given canvas and brush positions 
        /// relative to each sprite's pivots.
        /// 
        /// Returns false if the two sprites don't overlap.
        /// </summary>
        public bool Blend(Sprite brush,
                          Blend<TPixel> blend,
                          IntVector2 canvasPosition = default(IntVector2),
                          IntVector2 brushPosition = default(IntVector2))
        {
            // convert both sprite texture-space rects into world-space coordinates
            var canvas = this;

            var b_offset = brushPosition - brush.pivot;
            var c_offset = canvasPosition - canvas.pivot;

            var world_rect_brush = new IntRect(b_offset.x,
                                               b_offset.y,
                                               brush.rect.width,
                                               brush.rect.height);

            var world_rect_canvas = new IntRect(c_offset.x,
                                                c_offset.y,
                                                canvas.rect.width,
                                                canvas.rect.height);

            // find the overlap of world-space rects
            var activeRect = world_rect_brush.Intersect(world_rect_canvas);

            if (activeRect.width < 1 || activeRect.height < 1)
            {
                return false;
            }

            // convert world-space overlap into texture-space rects
            IntRect local_rect_brush = activeRect;
            local_rect_brush.Move(-world_rect_brush.xMin + brush.rect.xMin,
                                  -world_rect_brush.yMin + brush.rect.yMin);

            IntRect local_rect_canvas = activeRect;
            local_rect_canvas.Move(-world_rect_canvas.xMin + canvas.rect.xMin,
                                   -world_rect_canvas.yMin + canvas.rect.yMin);

            // blend one rect of texture data onto the other
            canvas.mTexture.Blend(brush.mTexture, blend, local_rect_canvas, local_rect_brush);

            return true;
        }

        /// <summary>
        /// Replace all pixels of this sprite that are overlapped by another with 
        /// either a given pixel value or the default pixel value.
        /// 
        /// The same behaviour as Blend except instead of blending the pixel values
        /// are set to a constant.
        /// </summary>
        public bool Crop(Sprite bounds,
                         IntVector2 canvasPosition = default(IntVector2),
                         IntVector2 brushPosition = default(IntVector2),
                         TPixel value = default(TPixel))
        {
            var canvas = this;

            var b_offset = brushPosition - bounds.pivot;
            var c_offset = canvasPosition - canvas.pivot;

            var world_rect_brush = new IntRect(b_offset.x,
                                               b_offset.y,
                                               bounds.rect.width,
                                               bounds.rect.height);

            var world_rect_canvas = new IntRect(c_offset.x,
                                                c_offset.y,
                                                canvas.rect.width,
                                                canvas.rect.height);

            var activeRect = world_rect_brush.Intersect(world_rect_canvas);

            if (activeRect.width < 1 || activeRect.height < 1)
            {
                return false;
            }

            IntRect local_rect_canvas = activeRect;
            local_rect_canvas.Move(-world_rect_canvas.xMin + canvas.rect.xMin,
                                   -world_rect_canvas.yMin + canvas.rect.yMin);

            canvas.Crop(local_rect_canvas, value);

            return true;
        }

        /// <summary>
        /// Replace all pixels of this sprite within the given bounds with either 
        /// a given pixel value or the default pixel value.
        /// </summary>
        private void Crop(IntRect bounds, TPixel value = default(TPixel))
        {
            int stride = mTexture.width;

            int xmin = rect.xMin;
            int ymin = rect.yMin;
            int xmax = rect.xMax;
            int ymax = rect.yMax;

            for (int y = ymin; y < ymax; ++y)
            {
                for (int x = xmin; x < xmax; ++x)
                {
                    // TODO: instead of checking the bounds, just work out the
                    // correct rect in the first place
                    if (!bounds.Contains(x, y))
                    {
                        int i = y * stride + x;

                        mTexture.pixels[i] = value;
                    }
                }
            }

            mTexture.dirty = true;
        }

        /// <summary>
        /// Replace all pixels of this sprite with either a given pixel value or 
        /// the default pixel value.
        /// </summary>
        public void Clear(TPixel value = default(TPixel))
        {
            // a sprite is just a rect of pixels on an underlying texture
            mTexture.Clear(value, rect);
        }

        /// <summary>
        /// Return the pixel value of this sprite at the given coordinates, 
        /// relative to the sprite's pivot. If the coordinates are out of bounds,
        /// return the given default pixel value or the default pixel value.
        /// </summary>
        public TPixel GetPixel(int x, int y, TPixel @default = default(TPixel))
        {
            // translate coodinates relative to sprite pivot to coordinates
            // relative to texture bottom left
            x += rect.x + pivot.x;
            y += rect.y + pivot.y;

            if (rect.Contains(x, y))
            {
                return mTexture.GetPixel(x, y);
            }
            else
            {
                return @default;
            }
        }

        /// <summary>
        /// Set the pixel value of this sprite at the given coordinates, relative
        /// to the sprite's pivot. Return true if the coordinates are in bounds,
        /// do nothing and return false if they aren't.
        /// </summary>
        public bool SetPixel(int x, int y, TPixel value)
        {
            // translate coodinates relative to sprite pivot to coordinates
            // relative to texture bottom left
            x += rect.x + pivot.x;
            y += rect.y + pivot.y;

            if (rect.Contains(x, y))
            {
                mTexture.SetPixel(x, y, value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Set the pixel value of this sprite at the given coordinates, relative
        /// to the sprite's bottom left. Return true if the coordinates are in 
        /// bounds, do nothing and return false if they aren't.
        /// </summary>
        public bool SetPixelAbsolute(int x, int y, TPixel value)
        {
            // translate coordinates within sprite rect into coordinates relative
            // to texture bottom left
            x += rect.x;
            y += rect.y;

            if (rect.Contains(x, y))
            {
                mTexture.SetPixel(x, y, value);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply new pixel changes to the underlying Unity Texture
        /// </summary>
        public void Apply()
        {
            // a sprite is just a rect of pixels on an underlying texture - that
            // texture needs to be applied
            mTexture.Apply();
        }

        /// <summary>
        /// Destroy the corresponding Unity Sprite, if it was ever created. They
        /// do not get garbage collected!
        /// </summary>
        public void Dispose()
        {
            if (_uSprite != null)
            {
                UnityEngine.Object.Destroy(_uSprite);
            }
        }

        /// <summary>
        /// Set the Pixels Per Unit of the corresponding Unity Sprite. The previous
        /// Unity Sprite is destroyed and replaced in the process.
        /// </summary>
        public void SetPixelsPerUnit(float ppu)
        {
            // we no longer need the existing Unity Sprite
            Dispose();

            // we can't create the sprite unless we force the texture to exist
            mTexture.Apply();

            // recreate the sprite (FullRect stops unity trying cut transparent corners off)
            uSprite = UnityEngine.Sprite.Create(mTexture.uTexture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
        }
    
        /// <summary>
        /// Return an array of all the pixel values in this sprite. If an array is
        /// provided then it will be filled and returned instead of creating a new
        /// one.
        /// </summary>
        public TPixel[] GetPixels(TPixel[] destination=null)
        {
            // a sprite is just a rect of pixels on an underlying texture
            return mTexture.GetPixels(rect, destination);
        }

        /// <summary>
        /// Overwrite all pixel values in this sprite with those in a given array.
        /// </summary>
        public void SetPixels(TPixel[] pixels)
        {
            // a sprite is just a rect of pixels on an underlying texture
            mTexture.SetPixels(rect, pixels);
        }
    }
}
