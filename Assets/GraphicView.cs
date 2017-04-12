using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using Random = UnityEngine.Random;

public class GraphicView : InstanceView<FlatGraphic>
{
    [SerializeField]
    private Main main;
    [SerializeField]
    private Image image;

    public override void Refresh()
    {
        image.sprite = main.GetImageResource(config.graphicURI).sprite;

        transform.position = config.position;
        transform.localScale = config.scale * Vector3.one;
        transform.localEulerAngles = config.direction * Vector3.forward;
    }
}
