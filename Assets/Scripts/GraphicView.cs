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

        //image.color = (config.pinned && !main.playing) ? Color.white * 0.75f : Color.white;

        image.sprite = main.GetImageSprite(config.graphicURI);
        image.alphaHitTestMinimumThreshold = 0.25f;
        image.SetNativeSize();

        Vector3 scale = config.scale * Vector3.one * (selected ? Mathf.Lerp(0.975f, 1.025f, u) : 1);
        scale.z = 1;

        transform.localPosition = (Vector3) config.position + Vector3.back * config.depth;
        transform.localScale = scale;
        transform.localEulerAngles = config.direction * Vector3.forward;
    }
}
