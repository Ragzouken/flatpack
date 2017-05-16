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

    [SerializeField]
    private UnityEngine.UI.RawImage previewImage;

    private WebCamTexture webcam;

    private Sprite image;
    private TextureByte.Sprite mask;
    private GraphicSource source;
    private Coroutine loading;

    private TextureByte.Sprite brush;

    private void Awake()
    {
        brush = TextureByte.Draw.Circle(64, 255);
        webcam = new WebCamTexture(512, 512, 60);

        previewImage.texture = webcam;
        webcam.Play();
    }

    public void Close()
    {
        if (webcam != null)
        {
            webcam.Stop();
        }

        gameObject.SetActive(false);

        Reset();
    }

    public void Open()
    {
        if (webcam != null)
        {
            webcam.Play();
        }
    }

    public void TakePhoto()
    {
        webcam.Pause();
    }

    public void RetryPhoto()
    {
        webcam.Play();
    }

    public void CancelPhoto()
    {
        Close();
    }

    public void AcceptPhoto()
    {
        Color32 clear = Color.clear;

        var pix = webcam.GetPixels32();
        var next = new Texture2D(webcam.width, webcam.height, TextureFormat.ARGB32, false);

        /*
        for (int i = 0; i < pix.Length; ++i)
        {
            if (mask.mTexture.pixels[i] > 128)
            {
                pix[i] = clear;
            }
        }
        */

        next.SetPixels32(pix);
        
        string root = "/storage/emulated/0/DCIM/";
        string name = "import-test-" + Guid.NewGuid() + ".png";

        System.IO.File.WriteAllBytes(root + name, next.EncodeToPNG());

        main.StartCoroutine(main.LoadFromFile(root + name));

        Close();
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

        mask.Apply();

        sourceImage.sprite = image;
        sourceImage.SetNativeSize();
        maskImage.sprite = mask.uSprite;
        maskImage.SetNativeSize();

        loadingBlocker.SetActive(false);
    }

    public void Import()
    {
        Color32 clear = Color.clear;

        var pix = image.texture.GetPixels32();
        var next = new Texture2D(image.texture.width, image.texture.height, TextureFormat.ARGB32, false);

        for (int i = 0; i < pix.Length; ++i)
        {
            if (mask.mTexture.pixels[i] > 128)
            {
                pix[i] = clear;
            }
        }

        next.SetPixels32(pix);
        
        string root = "/storage/emulated/0/DCIM/";
        string name = "import-test-" + Guid.NewGuid() + ".png";

        System.IO.File.WriteAllBytes(root + name, next.EncodeToPNG());

        main.StartCoroutine(main.LoadFromFile(root + name));
    }

    private bool dragging;
    private Vector2 prev;

    private void Update()
    {
        return;

        if (Input.GetMouseButton(0))
        {
            Vector2 next;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(maskImage.rectTransform,
                                                                    Input.mousePosition,
                                                                    null,
                                                                    out next);

            next += new Vector2(mask.rect.width, mask.rect.height) * 0.5f;

            if (dragging)
            {
                var sweep = TextureByte.Draw.Sweep(brush, prev, next, TextureByte.mask);

                mask.Blend(sweep, Blend);
                mask.Apply();

                TextureByte.Draw.FreeSprite(sweep);
            }

            prev = next;

            dragging = true;
        }
        else
        {
            dragging = false;
        }
    }
}
