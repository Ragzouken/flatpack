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
    private Image sourceImage, maskImage;

    [SerializeField]
    private UnityEngine.UI.RawImage previewImage;

    private WebCamTexture webcam;
    
    private TextureByte.Sprite mask;
    private Coroutine loading;

    private TextureByte.Sprite brush;

    private void Awake()
    {
        brush = TextureByte.Draw.Circle(64, 255);
        webcam = new WebCamTexture(512, 512, 60);

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

        acceptButton.interactable = true;
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
        var next = new Texture2D(webcam.width, webcam.height, TextureFormat.ARGB32, false);

        for (int i = 0; i < pix.Length; ++i)
        {
            if (mask.mTexture.pixels[i] > 128)
            {
                pix[i] = clear;
            }
        }

        next.SetPixels32(pix);
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

        if (webcam != null)
        {

        }

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
        if (mask == null)
        {
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
