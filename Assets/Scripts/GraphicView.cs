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
    public const float pulseSpeed = 2;

    [SerializeField]
    private Main main;
    [SerializeField]
    private Image image;

    public override void Refresh()
    {
        bool selected = main.selected == config;

        float u = (Time.timeSinceLevelLoad * pulseSpeed) % 1;
        u = Mathf.Sin(u * Mathf.PI * 2) * 0.5f + 0.5f;

        image.sprite = main.GetImageResource(config.graphicURI).sprite;
        image.alphaHitTestMinimumThreshold = 0.25f;
        image.SetNativeSize();

        transform.position = (Vector3) config.position + Vector3.back * config.depth;
        transform.localScale = config.scale * Vector3.one * (selected ? Mathf.Lerp(0.975f, 1.025f, u) : 1);
        transform.localEulerAngles = config.direction * Vector3.forward;
    }
}
