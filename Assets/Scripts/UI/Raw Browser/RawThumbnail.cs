using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class RawThumbnail : InstanceView<GraphicSource> 
{
    [SerializeField]
    private RawBrowserPanel panel;
    [SerializeField]
    private Button selectButton;
    [SerializeField]
    private Image thumbnailImage;
    [SerializeField]
    private Text nameText;

    private void Awake()
    {
        selectButton.onClick.AddListener(() => panel.Select(config));
    }

    public override void Refresh()
    {
        thumbnailImage.sprite = config.thumbnail;
    }
}
