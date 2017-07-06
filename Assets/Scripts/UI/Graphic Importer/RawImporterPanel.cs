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

    [Header("Buttons")]
    [SerializeField]
    private Button captureButton;
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private Button acceptButton;
    [SerializeField]
    private Button cancelButton;

    [Header("Prompts")]
    [SerializeField]
    private GameObject capturePrompt;
    [SerializeField]
    private GameObject scrubPrompt;

    [SerializeField]
    private Image maskImage;

    [SerializeField]
    private AspectRatioFitter fitter;
    [SerializeField]
    private RawImage previewImage;
    [SerializeField]
    private AudioSource scrubSound;

    private WebCamTexture webcam;
    
    private TextureByte.Sprite mask;
    private Coroutine loading;

    private TextureByte.Sprite brush;

    private void Awake()
    {
        brush = TextureByte.Draw.Circle(64, 255);
        webcam = new WebCamTexture();

        previewImage.texture = webcam;
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

    private void OnEnable()
    {
        RetryPhoto();
    }

    public void TakePhoto()
    {
        webcam.Pause();

        maskImage.gameObject.SetActive(true);

        mask = TextureByte.Draw.GetSprite(webcam.width, webcam.height);
        mask.SetPixelsPerUnit(100);
        mask.Clear(224);

        mask.Apply();
        maskImage.sprite = mask.uSprite;
        //maskImage.SetNativeSize();

        acceptButton.interactable = false;
        captureButton.interactable = false;
        capturePrompt.SetActive(false);
        scrubPrompt.SetActive(true);
    }

    public void RetryPhoto()
    {
        Reset();

        webcam.Play();

        acceptButton.interactable = false;
        captureButton.interactable = true;
        capturePrompt.SetActive(true);
    }

    public void CancelPhoto()
    {
        Close();
    }

    public void AcceptPhoto()
    {
        Color32 clear = Color.clear;

        var pix = webcam.GetPixels32();

        int min_x = webcam.width, min_y = webcam.height, max_x = 0, max_y = 0;

        for (int y = 0; y < webcam.height; ++y)
        {
            for (int x = 0; x < webcam.width; ++x)
            {
                int i = y * webcam.width + x;

                if (mask.mTexture.pixels[i] <= 128)
                {
                    min_x = Mathf.Min(x, min_x);
                    min_y = Mathf.Min(y, min_y);
                    max_x = Mathf.Max(x, max_x);
                    max_y = Mathf.Max(y, max_y);
                }
            }
        }

        //Debug.LogFormat("{0} {1} {2} {3}", min_x, min_y, max_x, max_y);

        for (int i = 0; i < pix.Length; ++i)
        {
            if (mask.mTexture.pixels[i] > 128)
            {
                pix[i] = clear;
            }
        }

        var next = new Texture2D(max_x - min_x + 1, 
                                 max_y - min_y + 1, 
                                 TextureFormat.ARGB32, 
                                 false);

        var pixels = next.GetPixels32();

        {
            int ti = 0;

            for (int y = min_y; y <= max_y; ++y)
            {
                for (int x = min_x; x <= max_x; ++x)
                {
                    int i = y * mask.mTexture.width + x;

                    pixels[ti] = pix[i];
                    ti += 1;
                }
            }
        }

        next.SetPixels32(pixels);
        next.Apply();

        string id = Guid.NewGuid().ToString();
        string root = Application.persistentDataPath + "/imported/";
        string name = id + ".png";

        System.IO.Directory.CreateDirectory(root);
        System.IO.File.WriteAllBytes(root + name, next.EncodeToPNG());

        main.InsertImported(id, next);
        main.Save();

        Close();
    }

    public void Reset()
    {
        capturePrompt.SetActive(false);
        scrubPrompt.SetActive(false);
        scrubSound.Stop();

        maskImage.gameObject.SetActive(false);

        if (mask != null)
        {
            TextureByte.Draw.FreeSprite(mask);
            mask = null;
        }
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

    private bool dragging;
    private Vector2 prev;

    private void Update()
    {
        if (webcam != null)
        {
            float w = webcam.width;
            float h = webcam.height;

            fitter.aspectRatio = w / h;
        }

        if (mask == null)
        {
            scrubSound.Stop();
            dragging = false;

            return;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 next;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(maskImage.rectTransform,
                                                                    Input.mousePosition,
                                                                    null,
                                                                    out next);

            next += new Vector2(maskImage.rectTransform.rect.width, maskImage.rectTransform.rect.height) * 0.5f;

            next.x = (next.x / maskImage.rectTransform.rect.width) * webcam.width;
            next.y = (next.y / maskImage.rectTransform.rect.height) * webcam.height;

            if (dragging)
            {
                var sweep = TextureByte.Draw.Sweep(brush, prev, next, TextureByte.mask);

                mask.Blend(sweep, Blend);
                mask.Apply();

                TextureByte.Draw.FreeSprite(sweep);

                acceptButton.interactable = true;
            }

            prev = next;

            dragging = true;
        }
        else
        {
            dragging = false;
        }

        if (scrubSound.isPlaying && !dragging)
        {
            scrubSound.Stop();
        }
        else if (!scrubSound.isPlaying && dragging)
        {
            scrubSound.Play();
        }
    }
}
