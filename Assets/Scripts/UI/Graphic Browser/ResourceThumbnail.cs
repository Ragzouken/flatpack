using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class ResourceThumbnail : InstanceView<ImageResource> 
{
    [SerializeField]
    private GraphicBrowserPanel panel;
    [SerializeField]
    private Image image;
    [SerializeField]
    private Button button;

    private void Awake()
    {
        button.onClick.AddListener(() => panel.Choose(config));
    }

    public override void Refresh()
    {
        image.sprite = config.sprite;
    }
}
