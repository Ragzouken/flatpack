using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class RawImporterPanel : MonoBehaviour
{
    [SerializeField]
    private Main main;

    [SerializeField]
    private GameObject loadingBlocker;

    [SerializeField]
    private Image sourceImage, maskImage;

    private Sprite image;
    private TextureByte.Sprite mask;
    private GraphicSource source;
    private Coroutine loading;

    private TextureByte.Sprite brush;

    private void Awake()
    {
        brush = TextureByte.Draw.Circle(32, 32);
    }

    public void Reset()
    {
        if (loading != null)
        {

        }

        if (source != null)
        {

        }

        if (mask != null)
        {
            TextureByte.Draw.FreeSprite(mask);
        }

        source = null;
    }

    public void OpenSource(GraphicSource source)
    {
        Reset();

        this.source = source;

        loading = main.StartCoroutine(LoadSource());

        
    }

    private static byte Blend(byte canvas, byte brush)
    {
        if (canvas > brush)
        {
            return (byte) (canvas - brush);
        }
        else
        {
            return 0;
        }
    }

    private IEnumerator LoadSource()
    {
        loadingBlocker.SetActive(true);

        var load = new WWW("file://" + source.path);

        yield return load;

        var texture = load.texture;

        TextureScale.BilinearMax(texture, 640, 640);
        
        image = texture.FullSprite(pixelsPerUnit: 100);
        mask = TextureByte.Draw.GetSprite((int) image.rect.width, (int) image.rect.height);
        mask.SetPixelsPerUnit(100);
        mask.Clear(224);

        for (int i = 0; i < 100; ++i)
        {
            mask.Blend(brush, Blend, brushPosition: IntVector2.one * (int) (256 * Random.value + 128));
        }

        mask.Apply();

        sourceImage.sprite = image;
        sourceImage.SetNativeSize();
        maskImage.sprite = mask.uSprite;
        maskImage.SetNativeSize();

        loadingBlocker.SetActive(false);
    }

    public void Import()
    {

    }
}
