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
    private Image image;

    protected override void Configure()
    {
        Refresh();
    }

    public override void Refresh()
    {
        image.sprite = config.sprite;
    }
}
